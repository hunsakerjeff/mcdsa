using System;
using System.Collections.ObjectModel;
using DSA.Model.Messages;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;

namespace DSA.Shell.ViewModels.Playlist
{
    public class NewPlaylistViewModel : ViewModelBase
    {
        private string _name;
        private readonly ObservableCollection<PlaylistViewModel> _collection;
        private RelayCommand _createNewPlaylistCommand;
        private readonly NavigateToMediaCommand _navigationCommand;
        private readonly IDialogService _dialogSevice;
        private bool _isFlyoutOpen;
        private readonly Action<PlaylistViewModel> _removeMethod;

        public NewPlaylistViewModel(
            ObservableCollection<PlaylistViewModel> collection, 
            NavigateToMediaCommand navigationCommand, 
            IDialogService dialogSevice,
            Action<PlaylistViewModel> removeMethod)
        {
            _removeMethod = removeMethod;
            _collection = collection;
            _dialogSevice = dialogSevice;
            _navigationCommand = navigationCommand;
        }

        public String Name 
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

        public bool IsFlyoutOpen
        {
            get
            {
                return _isFlyoutOpen;
            }
            set
            {
                Set(ref _isFlyoutOpen, value);
                Name = string.Empty;
            }
        }

        public RelayCommand CreateNewPlaylistCommand
        {
            get
            {
                return _createNewPlaylistCommand ?? (_createNewPlaylistCommand = new RelayCommand(
                    () =>
                    {
                        var newPlaylistData = PlaylistViewModelBuilder.CreateNew(Name, _navigationCommand, _dialogSevice);
                        newPlaylistData.IsPlaylistEditMode = true;
                        newPlaylistData.RemoveMethod = () => _removeMethod(newPlaylistData);
                        _collection.Add(newPlaylistData);
                        IsFlyoutOpen = false;
                        Messenger.Default.Send(new NewPlaylistAddedMessage());
                    },
                    () =>
                    {
                        return string.IsNullOrWhiteSpace(Name) == false;
                    }
                    ));
            }
        }
    }
}