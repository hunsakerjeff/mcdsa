using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using DSA.Common.Utils;
using DSA.Data.Interfaces;
using DSA.Model.Messages;
using DSA.Shell.ViewModels.Abstract;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar;
using DSA.Shell.Commands;

namespace DSA.Shell.ViewModels.Common
{
    public class MainAppBarViewModel : DSAViewModelBase
    {
        private readonly IMobileConfigurationDataService _dataService;
        private readonly INavigationService _navigationService;

        private ImageSource _logoImage;
        private ImageSource _homeImage;
        private ImageSource _visualBrowserImage;
        private ImageSource _menuBrowserImage;
        private ImageSource _historyImage;
        private ImageSource _playlistImage;
        private ImageSource _spotlightImage;

        private bool _visualBrowserCheck;
        private bool _menuBrowserCheck;
        private bool _historyCheck;
        private bool _playlistCheck;
        private bool _spotlightCheck;
        private bool _isConfigurationSelected;


        public MainAppBarViewModel(
           IMobileConfigurationDataService dataService,
           INavigationService navigationService,
           ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _dataService = dataService;
            _navigationService = navigationService;

            Initialize();
        }

        protected override async Task Initialize()
        {
            try
            {
                DispatcherHelper.CheckBeginInvokeOnUI(
                async () =>
                {
                    LogoImage = await _dataService.GetLogoImage();
                    var currentConfig = await SettingsDataService.GetCurrentMobileConfigurationID();
                    IsConfigurationSelected = string.IsNullOrWhiteSpace(currentConfig) == false;
                    RefreshCommands();
                    RefreshCheckState();
                }
              );
            }
            catch (Exception ex)
            {
                // Report error here
            }
        }

        private void RefreshCommands()
        {
            NavigateMenuBrowserCommand.RaiseCanExecuteChanged();
            NavigateHistoryCommand.RaiseCanExecuteChanged();
            NavigatePlaylistCommand.RaiseCanExecuteChanged();
            NavigateSpotlightCommand.RaiseCanExecuteChanged();
        }

        private void RefreshCheckState()
        {
            System.Diagnostics.Debug.WriteLine("Refreshing CheckState");
            System.Diagnostics.Debug.WriteLine(_navigationService.CurrentPageKey);

            VisualBrowserCheck = _navigationService.CurrentPageKey == ViewModelLocator.VisualBrowserPageKey || _navigationService.CurrentPageKey == ViewModelLocator.SearchPageKey || _navigationService.CurrentPageKey == "-- ROOT --";
            MenuBrowserCheck = _navigationService.CurrentPageKey == ViewModelLocator.MenuBrowserPageKey;
            HistoryCheck =  _navigationService.CurrentPageKey == ViewModelLocator.HistoryPageKey;
            PlaylistCheck = _navigationService.CurrentPageKey == ViewModelLocator.PlaylistPageKey;
            SpotlightCheck = _navigationService.CurrentPageKey == ViewModelLocator.SpotlightPageKey;
        }

        public ImageSource LogoImage
        {
            get
            {
                return _logoImage;
            }
            set
            {
                Set(ref _logoImage, value);
            }
        }

        private RelayCommand _navigateVisualBrowserCommand;
        public RelayCommand NavigateVisualBrowserCommand
        {
            get
            {
                return _navigateVisualBrowserCommand ?? (_navigateVisualBrowserCommand = new RelayCommand(() =>
                {
                    _navigationService.NavigateTo(ViewModelLocator.VisualBrowserPageKey);

                    RefreshCheckState();
                }));
            }
        }

        private RelayCommand _navigateVisualBrowserTopLevelCommand;
        public RelayCommand NavigateVisualBrowserTopLevelCommand
        {
            get
            {
                return _navigateVisualBrowserTopLevelCommand ?? (_navigateVisualBrowserTopLevelCommand = new RelayCommand(() =>
                {
                    _navigationService.NavigateTo(ViewModelLocator.VisualBrowserPageKey);
                    Messenger.Default.Send(new NavigateVisualBrowserTopLevelMessage());
                    RefreshCheckState();
                }));
            }
        }

        private RelayCommand _navigateMenuBrowserCommand;
        public RelayCommand NavigateMenuBrowserCommand
        {
            get
            {
                return _navigateMenuBrowserCommand ?? (_navigateMenuBrowserCommand = new RelayCommand(
                    () =>
                    {
                        _navigationService.NavigateTo(ViewModelLocator.MenuBrowserPageKey);
                        RefreshCheckState();
                    }, 
                    () => IsConfigurationSelected));
            }
        }

        private RelayCommand _navigateHistoryCommand;
        public RelayCommand NavigateHistoryCommand
        {
            get
            {
                return _navigateHistoryCommand ?? (_navigateHistoryCommand = new RelayCommand(
                    () =>
                    {
                        _navigationService.NavigateTo(ViewModelLocator.HistoryPageKey);
                        RefreshCheckState();
                    },
                    () => IsConfigurationSelected));
            }
        }

        private RelayCommand _navigatePlaylistCommand;
        public RelayCommand NavigatePlaylistCommand
        {
            get
            {
                return _navigatePlaylistCommand ?? (_navigatePlaylistCommand = new RelayCommand(
                    () =>
                    {
                        _navigationService.NavigateTo(ViewModelLocator.PlaylistPageKey);
                        RefreshCheckState();
                    },
                    () => IsConfigurationSelected));
            }
        }

        private RelayCommand _navigateSpotlightCommand;
        public RelayCommand NavigateSpotlightCommand
        {
            get
            {
                return _navigateSpotlightCommand ?? (_navigateSpotlightCommand = new RelayCommand(
                    () =>
                    {
                        _navigationService.NavigateTo(ViewModelLocator.SpotlightPageKey);
                        RefreshCheckState();
                    },
                    () => IsConfigurationSelected));
            }
        }

        public bool VisualBrowserCheck
        {
            get { return _visualBrowserCheck; }
            set { Set(ref _visualBrowserCheck, value); }
        }
        
        public bool MenuBrowserCheck {
            get { return _menuBrowserCheck; }
            set { Set(ref _menuBrowserCheck, value); }
        }

        public bool HistoryCheck
        {
            get { return _historyCheck; }
            set { Set(ref _historyCheck, value); }
        }


        public bool PlaylistCheck
        {
            get { return _playlistCheck; }
            set { Set(ref _playlistCheck, value); }
        }

        public bool SpotlightCheck
        {
            get { return _spotlightCheck; }
            set { Set(ref _spotlightCheck, value); }
        }

        public ImageSource HomeImage
        {
            get
            {
                return _homeImage ?? (_homeImage = ImageUtil.GetImageSouce("ms-appx:///Assets/AppBar/home@2x.png"));
            }
        }

        public ImageSource VisualBrowserImage
        {
            get
            {
                return _visualBrowserImage ?? (_visualBrowserImage = ImageUtil.GetImageSouce("ms-appx:///Assets/AppBar/visual_browser_tab@2x.png"));
            }
        }

        public ImageSource MenuBrowserImage
        {
            get
            {
                return _menuBrowserImage ?? (_menuBrowserImage = ImageUtil.GetImageSouce("ms-appx:///Assets/AppBar/browse_tab@2x.png"));
            }
        }

        public ImageSource HistoryImage
        {
            get
            {
                return _historyImage ?? (_historyImage = ImageUtil.GetImageSouce("ms-appx:///Assets/AppBar/history@2x.png"));
            }
        }

        public ImageSource PlaylistImage
        {
            get
            {
                return  _playlistImage ?? (_playlistImage = ImageUtil.GetImageSouce("ms-appx:///Assets/AppBar/appbar_icon2.png"));
            }
        }

        public ImageSource SpotlightImage
        {
            get
            {
                return _spotlightImage ?? (_spotlightImage = ImageUtil.GetImageSouce("ms-appx:///Assets/AppBar/spotlight-tab@2x.png"));
            }
        }

        public bool IsConfigurationSelected
        {
            get { return _isConfigurationSelected; }
            set { Set(ref _isConfigurationSelected, value); }
        }
    }
}