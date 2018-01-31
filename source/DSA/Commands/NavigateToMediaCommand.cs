using System;
using System.Windows.Input;
using DSA.Data.Interfaces;
using DSA.Model.Enums;
using DSA.Model.Messages;
using DSA.Shell.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;

namespace DSA.Shell.Commands
{
    public class NavigateToMediaCommand : ICommand
    {
        private readonly INavigationService _navigationService;
        private readonly IHistoryDataService _historyDataService;

        public NavigateToMediaCommand(INavigationService navigationService, IHistoryDataService historyDataService = null)
        {
            _navigationService = navigationService;
            _historyDataService = historyDataService;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var mediaContainer = parameter as IHaveMedia;
            if(mediaContainer == null)
            {
                return;
            }

            if(_historyDataService != null)
            {
                _historyDataService.AddToHistory(mediaContainer.Media);
            }

            Messenger.Default.Send(mediaContainer.Media);

            var playlistContainer = parameter as IHavePlaylist;
            if(playlistContainer != null)
            {
                Messenger.Default.Send(new PlaylistSelectedMessage(playlistContainer.PlaylistID));
            }

            _navigationService.NavigateTo(ViewModelLocator.MediaContentPageKey);
        }
    }
}
