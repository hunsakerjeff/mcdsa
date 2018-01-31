using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IPlaylistDataService
    {
        Task<List<PlaylistData>> GetPlayListsData();
        Task SaveChanges(List<PlaylistData> newData);
        Task<PlaylistData> GetPersonalLibrarData();
        Task<List<MediaLink>> GetPlayListMedia(string playlistID);
        Task<bool> AddToPlaylist(string iD, MediaLink _media);
        Task<bool> RemoveFromPlaylist(string iD, MediaLink _media);
        Task<List<PlaylistData>> GetUserPlaylists();
        Task<PlaylistData> CreateNew(string name);
    }
}
