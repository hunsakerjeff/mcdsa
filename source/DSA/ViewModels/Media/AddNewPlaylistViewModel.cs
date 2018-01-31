using System.Collections.ObjectModel;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Messages;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Shell.ViewModels.Media
{
    public class AddNewPlaylistViewModel : ViewModelBase
    {
        private string _name;
        private readonly ObservableCollection<MediaPlaylistViewModel> _collection;
        private readonly IPlaylistDataService _playlistDataService;
        private RelayCommand _createNewPlaylistCommand;
        private bool _buttonClicked;
        private MediaLink _media;
        private RelayCommand _createNewClickedCommand;
        private bool _isFlyoutOpen;

        public AddNewPlaylistViewModel(
            MediaLink media,
            IPlaylistDataService playlistDataService,
            ObservableCollection<MediaPlaylistViewModel> collection)
        {
            _media = media;
            _playlistDataService = playlistDataService;
            _collection = collection;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                Set(ref _name, value);
                _createNewPlaylistCommand?.RaiseCanExecuteChanged();
            }
        }

        public bool ButtonClicked
        {
            get { return _buttonClicked; }
            set { Set(ref _buttonClicked, value); }
        }

        public RelayCommand CreateNewClickedCommand
        {
            get
            {
                return _createNewClickedCommand ?? (_createNewClickedCommand = new RelayCommand(
                    () =>
                    {
                        ButtonClicked = true;
                        Name = string.Empty;
                    }));
            }
        }

       public RelayCommand CreateNewPlaylistCommand
        {
            get
            {
                return _createNewPlaylistCommand ?? (_createNewPlaylistCommand = new RelayCommand(
                    async () =>
                    {
                        var newPlaylist = await _playlistDataService.CreateNew(Name);

                        var newVm = MediaPlaylistViewModelBuilder.Create(_media, newPlaylist, _playlistDataService);
                        _collection.Add(newVm);

                        Messenger.Default.Send(new PlaylistChangedMessage());

                        ButtonClicked = false;
                    },
                    () =>
                    {
                        return string.IsNullOrWhiteSpace(Name) == false;
                    }
                    ));
            }
        }

        public bool IsFlyoutOpen
        {
            get
            {
                return _isFlyoutOpen;
            }
            set
            {
                Set(ref _isFlyoutOpen, value);
                if(value)
                {
                    Name = string.Empty;
                    ButtonClicked = false;
                }
            }
        }

    }
}