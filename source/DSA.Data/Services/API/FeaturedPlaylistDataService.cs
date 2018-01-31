using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Data.Services.API
{
    public class FeaturedPlaylistDataService : IFeaturedPlaylistDataService
    {
        public async Task<List<Playlist>> GetAllFeaturedPlaylists()
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("Playlist", "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore.Query(querySpec, 0).Select(item => CustomPrefixJsonConvert.DeserializeObject<Playlist>(item.ToString()))
                    .Where(pl => pl.__isDeleted == false)
                    .ToList();
            });
        }
        public async Task<List<PlaylistContent>> GetAllPlaylistContent()
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("PlaylistContent", "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore.Query(querySpec, 0).Select(item => CustomPrefixJsonConvert.DeserializeObject<PlaylistContent>(item.ToString())).ToList();
            });
        }

        public async Task<List<PlaylistContent>> GetPlaylistContent(IEnumerable<string> playlistIDs)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("PlaylistContent", "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                            .Query(querySpec, 0)
                            .Select(item => CustomPrefixJsonConvert.DeserializeObject<PlaylistContent>(item.ToString()))
                            .Where(c => (playlistIDs.Contains(c.PlasylistId)) && (c.__isDeleted == false))
                            .ToList();
            });
        }

        public Task<PlaylistContent> SavePlaylistContent(PlaylistContent playlistContent)
        {
            return Task<PlaylistContent>.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                var record = JObject.FromObject(playlistContent, JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));
                //remove data which should not be passed to salesforce
                record.Property("Id").Remove();
                record.Property("ContentId15").Remove();
                //set local variable
                record[Constants.Id] = Guid.NewGuid().ToString();//tmp id, we need it to maintain object relation (Id - it is salesforce tmp id, id - it is internal local id)
                record[SyncManager.Local] = true;
                record[SyncManager.LocallyCreated] = true;
                record[SyncManager.LocallyUpdated] = false;
                record[SyncManager.LocallyDeleted] = false;

                //for new record we have to set attributes.type property
                var prefix = SfdcConfig.CustomerPrefix;
                var info = playlistContent.GetType().GetTypeInfo().GetCustomAttributes()
                .SingleOrDefault(t => t is JsonObjectAttribute) as JsonObjectAttribute;
                if (info != null)
                {
                    record[Constants.SobjectType] = string.Format(info.Title, prefix);
                }
                JObject result = globalStore.Create("PlaylistContent", record, false);

                if (result != null)
                {
                    return CustomPrefixJsonConvert.DeserializeObject<PlaylistContent>(result.ToString());
                }
                else
                {
                    return null;
                }
            });
        }

        public Task<bool> RemovePlaylistContent(string playlistContentId, bool hardDelete = false)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                long internalId = globalStore.LookupSoupEntryId("PlaylistContent", Constants.Id, playlistContentId);
                if (internalId == -1)
                {
                    return false;//cannot find item in soup
                }
                var items = globalStore.Retrieve("PlaylistContent", new long[] { internalId });
                if (items.Count == 0)
                {
                    return false;//canot retrieve item from soup
                }
                var item = items[0].ToObject<JObject>();
                var isLocalObject = hardDelete || item.ExtractValue<bool>(SyncManager.LocallyCreated);
                if (isLocalObject)
                {//if it is only local object, we should remove it
                    return globalStore.Delete("PlaylistContent", new[] { internalId }, false);
                }
                else
                {//if it is not local object, we should mark it as deleted
                    item[SyncManager.Local] = true;
                    item[SyncManager.LocallyDeleted] = true;
                    var result = globalStore.Update("PlaylistContent", item, internalId, false);
                    return result != null;
                }
            });
        }

        public Task<bool> UpdatePlaylistContent(string playlistContentId, JObject updatedPlaylistContent)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                long internalId = globalStore.LookupSoupEntryId("PlaylistContent", Constants.Id, playlistContentId);
                if (internalId == -1)
                {
                    return false;//cannot find item in soup
                }
                var items = globalStore.Retrieve("PlaylistContent", internalId);
                if (items.Count == 0)
                {
                    return false;//canot retrieve item from soup
                }
                var item = items[0].ToObject<JObject>();
                
                //merge changes
                var mergeSettings = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                };
                item.Merge(updatedPlaylistContent);

                //mark record as updated locally
                item[SyncManager.Local] = true;
                item[SyncManager.LocallyUpdated] = true;

                var result = globalStore.Update("PlaylistContent", item, internalId, false);
                return result != null;
            });

        }
        public Task<Playlist> CreatePlaylist(Playlist playlist)
        {
            return Task<Playlist>.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                var record = JObject.FromObject(playlist, JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));
                //remove data which should not be passed to salesforce
                record.Property("Id").Remove();
                //set local variable
                record[Constants.Id] = Guid.NewGuid().ToString();//tmp id, we need it to maintain object relation (Id - it is salesforce tmp id, id - it is internal local id)
                record[SyncManager.Local] = true;
                record[SyncManager.LocallyCreated] = true;
                record[SyncManager.LocallyUpdated] = false;
                record[SyncManager.LocallyDeleted] = false;

                //for new record we have to set attributes.type property
                var prefix = SfdcConfig.CustomerPrefix;
                var info = playlist.GetType().GetTypeInfo().GetCustomAttributes()
                .SingleOrDefault(t => t is JsonObjectAttribute) as JsonObjectAttribute;
                if (info != null)
                {
                    record[Constants.SobjectType] = string.Format(info.Title, prefix);
                }

                JObject result = globalStore.Create("Playlist", record, false);

                if (result != null)
                {
                    return CustomPrefixJsonConvert.DeserializeObject<Playlist>(result.ToString());
                }
                else
                {
                    return null;
                }
            });
        }

        public Task<bool> RemovePlaylist(String playlistId)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                long internalId = globalStore.LookupSoupEntryId("Playlist", Constants.Id, playlistId);
                if (internalId == -1)
                {
                    return false;//cannot find item in soup
                }
                var items = globalStore.Retrieve("Playlist", internalId);
                if (items.Count == 0)
                {
                    return false;//canot retrieve item from soup
                }
                var item = items[0].ToObject<JObject>();
                var isLocalObject = Constants.ExtractValue<bool>(item, SyncManager.LocallyCreated);
                if (isLocalObject)
                {//if it is only local object, we should remove it
                    return globalStore.Delete("Playlist", new[] { internalId }, false);
                }
                else
                {//if it is not local object, we should mark it as deleted
                    item[SyncManager.Local] = true;
                    item[SyncManager.LocallyDeleted] = true;
                    var result = globalStore.Update("Playlist", item, internalId, false);
                    return result != null;
                }
            });
        }

        public Task<bool> UpdatePlaylist(string playlistId, JObject updatedPlaylist)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                long internalId = globalStore.LookupSoupEntryId("Playlist", Constants.Id, playlistId);
                if (internalId == -1)
                {
                    return false;//cannot find item in soup
                }
                var items = globalStore.Retrieve("Playlist", internalId);
                if (items.Count == 0)
                {
                    return false;//canot retrieve item from soup
                }
                var item = items[0].ToObject<JObject>();

                //merge changes
                var mergeSettings = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                };
                item.Merge(updatedPlaylist);

                //mark record as locally update
                item[SyncManager.Local] = true;
                item[SyncManager.LocallyUpdated] = true;

                var result = globalStore.Update("Playlist", item, internalId, false);
                return result != null;
            });
        }
    }
}
