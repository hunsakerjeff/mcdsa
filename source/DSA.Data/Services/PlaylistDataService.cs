using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Model.Models;
using DSA.Sfdc.Sync;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Adaptation;

namespace DSA.Data.Services
{
    public class PlaylistDataService : IPlaylistDataService
    {
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly IFeaturedPlaylistDataService _featuredPlaylistDataService;
        private readonly IUserSessionService _userSessionService;

        public PlaylistDataService(
          IDocumentInfoDataService documentInfoDataService,
          IFeaturedPlaylistDataService featuredPlaylistDataService,
          IUserSessionService userSessionService)
        {
            _documentInfoDataService = documentInfoDataService;
            _featuredPlaylistDataService = featuredPlaylistDataService;
            _userSessionService = userSessionService;
        }

        public async Task<PlaylistData> GetPersonalLibrarData()
        {
            var documents = await _documentInfoDataService.GetMyLibraryContentDocuments();
            return new PersonalPlaylistData()
            {
                PlaylistItems = documents.Select(d => new MediaLink(d)).ToList()
            };
        }

        public async Task<List<PlaylistData>> GetPlayListsData()
        {
            var playlists = await _featuredPlaylistDataService.GetAllFeaturedPlaylists();

            var playlistsIDs = playlists.Select(p => p.Id);
            var playlistsContent = await _featuredPlaylistDataService.GetPlaylistContent(playlistsIDs);

            var contentsIDs = playlistsContent.Select(pc => pc.ContentId15);
            var content = await _documentInfoDataService.GetContentDocumentsById15(contentsIDs);

            var documentsList = playlistsContent
                                     .Join(content, pc => pc.ContentId15, cd => cd.Document.Id15, (pc, cd) => new { PlaylistContent = pc, Document = cd });

            var userId = _userSessionService.GetCurrentUserId();

            return  playlists
                        .GroupJoin(documentsList, pl => pl.Id, dl => dl.PlaylistContent.PlasylistId, (pl, dl) => new PlaylistData { ID = pl.Id, __localId = pl.__localId, Name = pl.Name, IsEditable = pl.OwnerId == userId, OwnerId = pl.OwnerId, PlaylistItems = dl.Select(d => new MediaLink(d.Document, d.PlaylistContent)).OrderBy(d => d.Order).ToList() })
                        .OrderBy(pl => pl.Name)
                        .ToList();
        }

        public async Task SaveChanges(List<PlaylistData> newData)
        {
            List<PlaylistData> oldData = await GetPlayListsData();
            oldData = oldData.Where(pl => pl.IsEditable).ToList();
            var addedPlaylists = new List<PlaylistData>();
            var addedPlaylistItems = new List<PlaylistContent>();
            var updatedPlaylists = new List<PlaylistData>();
            var updatedPlaylistItems = new List<PlaylistContent>();
            var removedPlaylistsIds = new List<string>();
            var removedPlaylistItemsIds = new List<string>();
            var updatedOrTheSamePlaylistsIds = new List<string>();

            //
            //get all changes
            //
            foreach (var newPlaylist in newData)
            {
                var oldPlaylist = oldData.Find(oldObject => oldObject.ID == newPlaylist.ID);
                if (oldPlaylist == null)
                {
                    addedPlaylists.Add(newPlaylist);
                }
                else
                {
                    updatedOrTheSamePlaylistsIds.Add(oldPlaylist.ID);
                    //check if name has been changed
                    if (oldPlaylist.Name != newPlaylist.Name)
                    {
                        updatedPlaylists.Add(newPlaylist);
                    }
                    ComparePlaylistItems(newPlaylist, oldPlaylist, ref addedPlaylistItems, ref updatedPlaylistItems, ref removedPlaylistItemsIds);
                }
            }
            removedPlaylistsIds = oldData.Where(pl => !updatedOrTheSamePlaylistsIds.Contains(pl.ID)).Select(pl => pl.ID).ToList();

            //
            // persist data changes
            //
            //delete PlayList with content (hard delete of PlaylistContent, we don't want to sync deletion of the PlaylistConent if playlist has been deleted. It is handled in salesforce by object relation)
            foreach (var playlistID in removedPlaylistsIds)
            {
                bool success = await _featuredPlaylistDataService.RemovePlaylist(playlistID);
                if (success)
                {
                    var playlistsContent = await _featuredPlaylistDataService.GetPlaylistContent(new List<string>() { playlistID });
                    foreach (var playlistContent in playlistsContent)
                    {
                        bool contentSuccess = await _featuredPlaylistDataService.RemovePlaylistContent(playlistContent.Id, true);
                    }
                }
            }
            
            //delete PlaylistContent
            foreach (var playlistContentId in removedPlaylistItemsIds)
            {
                bool success = await _featuredPlaylistDataService.RemovePlaylistContent(playlistContentId);
            }

            //update playlist
            foreach (var playlist in updatedPlaylists)
            {
                dynamic updatedPlayList = new JObject();
                updatedPlayList.Name = playlist.Name;
                bool success = await _featuredPlaylistDataService.UpdatePlaylist(playlist.ID, updatedPlayList);
            }

            //update playlist content
            var prefix = SfdcConfig.CustomerPrefix;
            foreach (var playlistContent in updatedPlaylistItems)
            {
                dynamic updatedPlaylistContent = new JObject();
                updatedPlaylistContent[prefix + "__Order__c"] = playlistContent.Order;
                bool success = await _featuredPlaylistDataService.UpdatePlaylistContent(playlistContent.Id, updatedPlaylistContent);
            }

            //create new playlist
            foreach (var playlist in addedPlaylists)
            {
                var userId = _userSessionService.GetCurrentUserId();
                Playlist newPlaylist = new Playlist
                {
                    Name = playlist.Name,
                    IsFeatured = false,
                    OwnerId = userId
                };
                Playlist result = await _featuredPlaylistDataService.CreatePlaylist(newPlaylist);
                if (result != null)
                {
                    for (var idx = 0;  idx < playlist.PlaylistItems.Count; idx++)
                    {
                        addedPlaylistItems.Add(new PlaylistContent
                        {
                            PlasylistId = result.Id,
                            Name = playlist.PlaylistItems[idx].Name,
                            ContentId = playlist.PlaylistItems[idx].ID,
                            Order = idx
                        });
                    }
                        
                }
            }

            //create playlist content
            foreach (var playlistContent in addedPlaylistItems)
            {
                var result = await _featuredPlaylistDataService.SavePlaylistContent(playlistContent);
            }

            var res = await SyncUpNewPlaylist(true);
        }

        private double? getOrderValue(double? previousOrder, int newIdx)
        {
            double? orderValue = previousOrder;
            if (orderValue == null)
            {
                orderValue = newIdx;
            }
            else
            {
                orderValue++;
            }
            return orderValue;
        }

        private void ComparePlaylistItems(PlaylistData newPlaylist, PlaylistData oldPlaylist, ref List<PlaylistContent> addedPlaylistItems, ref List<PlaylistContent> updatedPlaylistItems, ref List<String> removedPlaylistItemsIds)
        {
            //compare playlist content
            double? previousOrder = null;
            bool changeOrder = false;
            var updatedOrTheSamePlaylistItemsIds = new List<string>();
            for (var newIdx = 0; newIdx < newPlaylist.PlaylistItems.Count; newIdx++)
            {
                var newPlaylistItem = newPlaylist.PlaylistItems[newIdx];
                var oldIdx = oldPlaylist.PlaylistItems.FindIndex(oldPlaylistItem => oldPlaylistItem.ID == newPlaylistItem.ID);
                if (oldIdx == -1)
                {
                    changeOrder = true;
                    double? orderValue = getOrderValue(previousOrder, newIdx);
                    previousOrder = orderValue;

                    addedPlaylistItems.Add(new PlaylistContent
                    {
                        PlasylistId = newPlaylist.ID,
                        Name = newPlaylistItem.Name,
                        ContentId = newPlaylistItem.ID,
                        Order = orderValue
                    });
                }
                else
                {
                    updatedOrTheSamePlaylistItemsIds.Add(newPlaylistItem.JunctionID);
                    if (oldIdx != newIdx || changeOrder)
                    {
                        changeOrder = true;//reorder all items after first item which doesn't match its index in old data
                        double? orderValue = getOrderValue(previousOrder, newIdx);
                        previousOrder = orderValue;

                        updatedPlaylistItems.Add(new PlaylistContent
                        {
                            Id = newPlaylistItem.JunctionID,
                            PlasylistId = newPlaylist.ID,
                            Name = newPlaylistItem.Name,
                            ContentId = newPlaylistItem.ID,
                            Order = orderValue
                        });
                    }
                    else
                    {
                        previousOrder = newPlaylistItem.Order;
                    }
                }
            }
            removedPlaylistItemsIds.AddRange(oldPlaylist.PlaylistItems.Where(pl => !updatedOrTheSamePlaylistItemsIds.Contains(pl.JunctionID)).Select(pl => pl.JunctionID));
        }

        public async Task<List<MediaLink>> GetPlayListMedia(string playlistID)
        {
            var list = await GetPlayListsData();
            var personal = await GetPersonalLibrarData();
            list.Add(personal);

            return list
                    .Where(pl => pl.ID == playlistID)
                    .SelectMany(pl => pl.PlaylistItems)
                    .ToList();
        }

        public async Task<bool> AddToPlaylist(string playlistID, MediaLink media)
        {
            //set order value, new content should be added at the end
            double? orderValue;
            var playlistsContent = await _featuredPlaylistDataService.GetPlaylistContent(new List<string>() { playlistID });
            if (playlistsContent.Any(pc => pc.Order != null))
            {
                orderValue = playlistsContent.Max(pc => pc.Order);
                orderValue++;
            }
            else
            {
                orderValue = playlistsContent.Count + 1;
            }

            PlaylistContent playlistContent = new PlaylistContent
            {
                ContentId = media.ID,
                PlasylistId = playlistID,
                Name = media.Name,
                Order = orderValue
            };
            var result = await _featuredPlaylistDataService.SavePlaylistContent(playlistContent);
            var returnValue = result != null;
            if (returnValue)
            {
                await SyncUpNewPlaylist(true);
            }

            return returnValue;
        }

        public async Task<bool> RemoveFromPlaylist(string playlistID, MediaLink media)
        {
            var result =  await _featuredPlaylistDataService.RemovePlaylistContent(media.JunctionID);
            await SyncUpNewPlaylist(true);
            return result;
        }

        public async Task<List<PlaylistData>> GetUserPlaylists()
        {
            var userId = _userSessionService.GetCurrentUserId();

            var list = await GetPlayListsData();
            return list.Where(pl => pl.OwnerId == userId).ToList();
        }

        public async Task<PlaylistData> CreateNew(string name)
        {
            var userId = _userSessionService.GetCurrentUserId();
            Playlist playlist = new Playlist
            {
                Name = name,
                IsFeatured = false,
                OwnerId = userId
            };

            Playlist result = await _featuredPlaylistDataService.CreatePlaylist(playlist);

            if (result == null)
            {
                return null;
            }
            await SyncUpNewPlaylist();
            var list = await GetPlayListsData();

            return list.First(pl => pl.__localId == result.__localId);
        }

        private async Task<bool> SyncUpNewPlaylist(bool syncPlaylistContent = false)
        {
            try
            {
                if (!ObjectSyncDispatcher.HasInternetConnection())
                    return false;
                
                await ObjectSyncDispatcher.Instance.SyncUpFeaturedPlaylists((text) => Debug.WriteLine(text));
                if (syncPlaylistContent)
                {
                    await ObjectSyncDispatcher.Instance.SyncUpPlaylistContent((text) => Debug.WriteLine(text));
                }
                return true;
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                Debug.WriteLine("SyncUpNewPlaylist exception");
            }
            return false;
        }
    }
}
