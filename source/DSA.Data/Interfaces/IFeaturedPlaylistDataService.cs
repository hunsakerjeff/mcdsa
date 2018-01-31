using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;
using Newtonsoft.Json.Linq;

namespace DSA.Data.Interfaces
{
    public interface IFeaturedPlaylistDataService
    {
        Task<List<Playlist>> GetAllFeaturedPlaylists();
        Task<List<PlaylistContent>> GetAllPlaylistContent();
        Task<List<PlaylistContent>> GetPlaylistContent(IEnumerable<string> playlistIDs);
        Task<PlaylistContent> SavePlaylistContent(PlaylistContent playlistContent);
        Task<bool> RemovePlaylistContent(string playlistContentId, bool hardDelete=false);
        Task<bool> UpdatePlaylistContent(string playlistContentId, JObject updatedPlaylistContent);
        Task<Playlist> CreatePlaylist(Playlist playlist);
        Task<bool> RemovePlaylist(String playlistId);
        Task<bool> UpdatePlaylist(string playlistId, JObject updatedPlaylist);
    }
}
