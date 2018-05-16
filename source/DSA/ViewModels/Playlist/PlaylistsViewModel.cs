using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model.Messages;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;
using WinRTXamlToolkit.Tools;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar;

namespace DSA.Shell.ViewModels.Playlist
{
    public class PlaylistsViewModel : DSAViewModelBase
    {
        private const string EditModeStateText = @"Done";
        private const string ReadOnlyModeStateText = @"Edit";

        private PlaylistViewModel _personalLibraryViewModel;
        private readonly IPlaylistDataService _playlistsDataService;
        private readonly INavigationService _navigationService;
        private readonly IHistoryDataService _historyDataService;
        private readonly IDialogService _dialogService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly ISearchContentDataService _searchContentDataService;
        private NewPlaylistViewModel _newPlaylistViewModel;
        private SearchControlViewModel _searchViewModel;

        private bool _isEdited;
        private string _editButtonText;
        private bool _editButtonDisabled;

        ObservableCollection<PlaylistViewModel> _playLists;
        private ObservableCollection<PlaylistViewModel> _allPlaylists;

        public PlaylistsViewModel(
            IPlaylistDataService playlistsDataService,
            INavigationService navigationService,
            IHistoryDataService historyDataService,
            IDialogService dialogService,
            ISearchContentDataService searchContentDataService,
            IDocumentInfoDataService documentInfoDataService,
            ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _playlistsDataService = playlistsDataService;
            _navigationService = navigationService;
            _historyDataService = historyDataService;
            _dialogService = dialogService;
            _documentInfoDataService = documentInfoDataService;
            _searchContentDataService = searchContentDataService;

            Initialize();
        }

        protected override void AttachMessages()
        {
            base.AttachMessages();
            Messenger.Default.
                Register<PlaylistChangedMessage>(this,
                    async (m) =>
                    {
                        await Initialize();
                    }
                    );
        }

        protected override async Task Initialize()
        {
            try
            {
                EditButtonDisabled = false;
                var navigationCommand = new NavigateToMediaCommand(_navigationService, _historyDataService);
                SearchViewModel = new SearchControlViewModel(_documentInfoDataService, _searchContentDataService, _navigationService, navigationCommand);

                var data = await _playlistsDataService.GetPlayListsData();
                _allPlaylists = PlaylistViewModelBuilder.Create(data, navigationCommand, _dialogService);
                PopulateRemoveCommand(_allPlaylists);
                SetInternalMode(_allPlaylists, IsInternalModeEnable);
                PlayLists = _allPlaylists;
                EditButtonText = ReadOnlyModeStateText;
                var personalLibrarydata = await _playlistsDataService.GetPersonalLibrarData();
                PersonalLibraryViewModel = PlaylistViewModelBuilder.Create(personalLibrarydata, navigationCommand, _dialogService);
                PersonalLibraryViewModel.IsInternalMode = IsInternalModeEnable;
                NewPlaylistViewModel = new NewPlaylistViewModel(this.PlayLists, new NavigateToMediaCommand(_navigationService, _historyDataService), _dialogService, (vm) => _allPlaylists.Remove(vm));
                EditButtonDisabled = true;
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }

        public SearchControlViewModel SearchViewModel
        {
            get { return _searchViewModel; }
            set { Set(ref _searchViewModel, value); }
        }

        private void SetInternalMode(ObservableCollection<PlaylistViewModel> _allPlaylists, bool isInternalModeEnable)
        {
            _allPlaylists.ForEach(pl => pl.IsInternalMode = isInternalModeEnable);
        }

        protected override void OnInternalModeChanged(bool value)
        {
            if(PlayLists == null)
            {
                return;
            }

            SetInternalMode(PlayLists, value);
            PersonalLibraryViewModel.IsInternalMode = value;
        }

        private void PopulateRemoveCommand(ObservableCollection<PlaylistViewModel> vmCollection)
        {
            vmCollection.ForEach(vm => vm.RemoveMethod = () => vmCollection.Remove(vm));
        }

        public PlaylistViewModel PersonalLibraryViewModel
        {
            get
            {
                return _personalLibraryViewModel;
            }
            set
            {
                Set(ref _personalLibraryViewModel, value);
            }
        }

        public bool IsPlaylistEditMode
        {
            get
            {
                return _isEdited;
            }
            set
            {
                Set(ref _isEdited, value);
                SetEditMode(_isEdited);
            }
        }

        private async void SetEditMode(bool isEditMode)
        {
            EditButtonText = isEditMode ? EditModeStateText : ReadOnlyModeStateText;
            PlayLists.ForEach(vm => vm.IsPlaylistEditMode = isEditMode);
            PersonalLibraryViewModel.IsPlaylistEditMode = isEditMode;
            if(isEditMode == false)
            {
                EditButtonDisabled = false;
                var newData = PlaylistViewModelBuilder.ConvertToModelData(this);
                await _playlistsDataService.SaveChanges(newData);
                await Initialize();
                EditButtonDisabled = true;
            }
        }

        public ObservableCollection<PlaylistViewModel> PlayLists
        {
            get { return _playLists; }
            set { Set(ref _playLists, value); }
        }


        public string EditButtonText
        {
            get { return _editButtonText; }
            set { Set(ref _editButtonText, value); }
        }

        public bool EditButtonDisabled
        {
            get { return _editButtonDisabled; }
            set { Set(ref _editButtonDisabled, value); }
        }
        
        public NewPlaylistViewModel NewPlaylistViewModel
        {
            get
            {
                return _newPlaylistViewModel;
            }
            set
            {
                Set(ref _newPlaylistViewModel, value);
            }
        }
    }
}