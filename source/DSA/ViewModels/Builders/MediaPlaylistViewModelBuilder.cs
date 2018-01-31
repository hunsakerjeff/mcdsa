using System.Collections.Generic;
using System.Linq;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Shell.ViewModels.Media;

namespace DSA.Shell.ViewModels.Builders
{
    public static class MediaPlaylistViewModelBuilder
    {
        internal static IEnumerable<MediaPlaylistViewModel> Create(MediaLink media, List<PlaylistData> playlists, IPlaylistDataService playlistDataService)
        {
            return playlists.Select(pl => new MediaPlaylistViewModel(pl, media, playlistDataService));
        }

        internal static MediaPlaylistViewModel Create(MediaLink media, PlaylistData playlist, IPlaylistDataService playlistDataService)
        {
            return new MediaPlaylistViewModel(playlist, media, playlistDataService);
        }
    }
}
