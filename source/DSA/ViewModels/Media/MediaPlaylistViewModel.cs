using System.Linq;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Shell.ViewModels.Media
{
    public class MediaPlaylistViewModel : ViewModelBase
    {
        private bool _isInPlaylist;
        private readonly PlaylistData _playlist;
        private readonly MediaLink _media;
             
        private RelayCommand _addRemoveFromPlaylist;
        private readonly IPlaylistDataService _playlistDataService;

        public MediaPlaylistViewModel(
             PlaylistData playlist,
             MediaLink media,
             IPlaylistDataService playlistDataService)
        {
            _playlist = playlist;
            _media = media;
            _playlistDataService = playlistDataService;
            IsInPlaylist = playlist.PlaylistItems.Any(m => m.ID == media.ID);
        }

        public string Name => _playlist.Name;

        public bool IsInPlaylist
        {
            get { return _isInPlaylist; }
            private set { Set(ref _isInPlaylist, value); }
        }

        public RelayCommand AddRemoveFromPlaylist
        {
            get
            {
                return _addRemoveFromPlaylist ?? (_addRemoveFromPlaylist = new RelayCommand(
                 async   () =>
                {
                    if(IsInPlaylist)
                    {
                        bool success = await _playlistDataService.RemoveFromPlaylist(_playlist.ID, _media);
                        if (success)
                        {
                            Messenger.Default.Send(new PlaylistChangedMessage());
                            IsInPlaylist = false;
                        }
                    }
                    else
                    {
                        bool success = await _playlistDataService.AddToPlaylist(_playlist.ID, _media);
                        if (success)
                        {
                            Messenger.Default.Send(new PlaylistChangedMessage());
                            IsInPlaylist = true;
                        }
                    }
                }));
            }
        }
    }
}