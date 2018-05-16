using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using DSA.Common.Utils;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Enums;
using DSA.Model.Messages;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar.CheckInOut;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar.Synchronization;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using WinRTXamlToolkit.Tools;
using DSA.Shell.Settings;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar
{
    public class ControlBarViewModel : DSAViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IMobileAppConfigDataService _mobileAppConfigDataService;
        private readonly IUserSessionService _userSessionService;
        private readonly IPresentationDataService _presentationDataService;
        private readonly IContactsService _contactsService;
        //private readonly ISearchContentDataService _searchContentDataService;
        private readonly ISyncLogService _syncLogService;
//        private readonly IDocumentInfoDataService _documentInfoDataService;
//        private readonly INavigationService _navigationService;

        //private readonly NavigateToMediaCommand _navigateToMediaCommand;
        private RelayCommand _logInLogOutCommand;
        private RelayCommand _showSynchronizationPopup;
        private RelayCommand _showDeltaSynchronizationPopup;
        private RelayCommand _showInitialSynchronizationPopup;
        private RelayCommand _enableDisableInternalMode;

        private RelayCommand _showHidePopup;
        private RelayCommand _reportProblemCommand;
        private RelayCommand _newContentShowDeltaSynchronizationPopupCommand;
        private RelayCommand _showAboutCommand;

        private string _logInLogOutText;
        private SynchronizationViewModel _synchronizationViewModel;
        private ObservableCollection<MobileConfigurationsSelectionViewModel> _mobileConfigurations;
        private bool _isSelectConfigurationPopupOpen;
        private readonly MobileConfigurationDTO _currentMobileConfiguration;

        private bool _newContentButtonVisibility;

        private readonly ImageSource _checkInIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/ico_check-in@2x.png");
        private readonly ImageSource _checkOutIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/ico_check-out@2x.png");
        private ImageSource _checkInOutIcon;

        private CheckInOutViewModel _checkInOutViewModel;

        private const string LogInText = "Sign In";
        private const string LogOutText = "Sign Out";

        private readonly ImageSource _logInIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/MenuIcons/about-btn-icon-signin@2x.png");
        private readonly ImageSource _logkOutIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/MenuIcons/about-btn-icon-signout@2x.png");
        private ImageSource _logInLogOutIcon;

        private bool _isAboutPopupOpen;
        private bool _isInternalModeEnableChecked;

        public ControlBarViewModel(
            IDialogService dialogService,
            ISettingsDataService settingsDataService,
            IMobileAppConfigDataService mobileAppConfigDataService,
            MobileConfigurationDTO currentMobileConfiguration,
            IUserSessionService userSessionService,
            IContactsService contactsService,
            IPresentationDataService presentationDataService,
            ISyncLogService syncLogService
            ) : base(settingsDataService)
        {
            _presentationDataService = presentationDataService;
            _userSessionService = userSessionService;
            _dialogService = dialogService;
            _mobileAppConfigDataService = mobileAppConfigDataService;
            _currentMobileConfiguration = currentMobileConfiguration;
            _contactsService = contactsService;
            _syncLogService = syncLogService;
            Initialize();
        }

        protected override async Task Initialize()
        {
            try
            {
                var currentID = await SettingsDataService.GetCurrentMobileConfigurationID();
                var configurations = await _mobileAppConfigDataService.GetMobileAppConfigs();

                var mcSelectViewModels = VisualBrowserViewModelBuilder.CreateMobileConfigurationsSelectionViewModel(configurations, SettingsDataService, currentID, () => { MobileConfigurations.ForEach(mc => mc.IsSelected = false); }, () => { IsSelectConfigurationPopupOpen = false; });
                MobileConfigurations = new ObservableCollection<MobileConfigurationsSelectionViewModel>(mcSelectViewModels);

                SetLogInOutStatus();

                IsInternalModeEnableChecked = IsInternalModeEnable;

                RefreshCommands();
                SetCheckInOutIcon(_presentationDataService);
                CheckInOutViewModel = new CheckInOutViewModel(_contactsService, _presentationDataService, SettingsDataService);
            }
            catch
            {
                //todo: log error
            }
        }

        private void SetLogInOutStatus()
        {
            var islogIn = _userSessionService.IsUserLogIn();
            LogInLogOutText = islogIn
                                ? LogOutText
                                : LogInText;

            LogInLogOutIcon = islogIn
                               ? _logkOutIcon
                               : _logInIcon;
        }

        private void SetCheckInOutIcon(IPresentationDataService presentationDataService)
        {
            var isPresenting = presentationDataService.IsPresentationStarted();
            CheckInOutIcon = isPresenting
                                ? _checkOutIcon
                                : _checkInIcon;
        }

        protected override void AttachMessages()
        {
            base.AttachMessages();
            Messenger.Default.Register<NewContentAvaliableMessage>(this, m =>
             DispatcherHelper.CheckBeginInvokeOnUI(() =>
             {
                 NewContentButtonVisibility = true;
             }));

            Messenger.Default.Register<SynchronizationCompleteMessage>(this, m =>
             DispatcherHelper.CheckBeginInvokeOnUI(() =>
             {
                 NewContentButtonVisibility = false;
                 Messenger.Default.Send(new StopStoryboardMessage { StoryboardName = "NewContentButtonSpin" });
             }));

            Messenger.Default.Register<CheckInOutChangedMessage>(this, m =>
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                SetCheckInOutIcon(_presentationDataService);
            }));
        }

        public RelayCommand ShowSynchronizationPopup
        {
            get
            {
                return _showSynchronizationPopup ?? (_showSynchronizationPopup = new RelayCommand(
                    () =>
                    {
                        SynchronizationViewModel.Mode = SynchronizationMode.Full;
                        SynchronizationViewModel.IsPopupOpen = true;
                    },
                    () =>
                    {
                        return (_userSessionService.IsUserLogIn() && !SettingsDataService.InSynchronizationInProgress);
                    }));
            }
        }

        public RelayCommand ShowDeltaSynchronizationPopup
        {
            get
            {
                return _showDeltaSynchronizationPopup ?? (_showDeltaSynchronizationPopup = new RelayCommand(
                    () =>
                    {
                        SynchronizationViewModel.Mode = SynchronizationMode.Delta;
                        SynchronizationViewModel.IsPopupOpen = true;
                    },
                    () =>
                    {
                        return (_userSessionService.IsUserLogIn() && !SettingsDataService.InSynchronizationInProgress);
                    }));
            }
        }

        public RelayCommand ShowInitialynchronizationPopup
        {
            get
            {
                return _showInitialSynchronizationPopup ?? (_showInitialSynchronizationPopup = new RelayCommand(
                    () =>
                    {
                        SynchronizationViewModel.Mode = SynchronizationMode.Initial;
                        SynchronizationViewModel.IsPopupOpen = true;
                    },
                    () =>
                    {
                        return (_userSessionService.IsUserLogIn() && !SettingsDataService.InSynchronizationInProgress);
            }));
            }
        }

        public SynchronizationViewModel SynchronizationViewModel
        {
            get
            {
                return _synchronizationViewModel ?? (_synchronizationViewModel = new SynchronizationViewModel(_dialogService, SettingsDataService, _syncLogService));
            }
        }

        public RelayCommand EnableDisableInternalMode
        {
            get
            {
                return _enableDisableInternalMode ?? (_enableDisableInternalMode = new RelayCommand(
                    () =>
                    {
                        if (IsInternalModeEnable == false)
                        {
                            _dialogService.ShowMessage("This information in this area is for internal personnel and authorized agent use only and may not be shown, printed or distributed (including email) outside of the company or used for sales or promotional purposes",
                                                        "Caution",
                                                        "Enable",
                                                        "Cancel",
                                                        (enable) =>
                                                        {
                                                            if (enable)
                                                            {
                                                                SettingsDataService.SetIsInternalModeEnable(true);
                                                            }
                                                            IsInternalModeEnableChecked = enable;
                                                        });
                        }
                        else
                        {
                            SettingsDataService.SetIsInternalModeEnable(false);
                        }
                    }));
            }
        }

        public bool IsInternalModeEnableChecked
        {
            get
            {
                return _isInternalModeEnableChecked;
            }
            set
            {
                Set(ref _isInternalModeEnableChecked, value);
            }
        }

        public RelayCommand LogInLogOutCommand
        {
            get
            {
                return _logInLogOutCommand ?? (_logInLogOutCommand = new RelayCommand(
                    async () =>
                    {

                        string message = null;
                        try
                        {
                            if (_userSessionService.IsUserLogIn())
                            {
                                var result = await _userSessionService.LogOut();
                                if (result)
                                {
                                    LogInLogOutText = LogInText;
                                }
                            }
                            else
                            {
                                await _userSessionService.LogIn();
                                LogInLogOutText = LogOutText;
                            }
                            RefreshCommands();
                        }
                        catch (Exception e)
                        {
                            message = e.Message;
                        }
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            var messageDialog = new MessageDialog(message, "Error");

                            await messageDialog.ShowAsync();
                        }
                    }));
            }
        }

        private void RefreshCommands()
        {
            ShowSynchronizationPopup.RaiseCanExecuteChanged();
            ShowDeltaSynchronizationPopup.RaiseCanExecuteChanged();
            ShowHideMobileConfigurationPopup.RaiseCanExecuteChanged();
        }

        public string LogInLogOutText
        {
            get { return _logInLogOutText; }
            set { Set(ref _logInLogOutText, value); }
        }

        public ImageSource LogInLogOutIcon
        {
            get { return _logInLogOutIcon; }
            set { Set(ref _logInLogOutIcon, value); }
        }

        public ImageSource SettingsIcon
        {
            get { return ImageUtil.GetImageSouce("ms-appx:///Assets/settings.png"); }
        }

        public RelayCommand ShowHideMobileConfigurationPopup
        {
            get
            {
                return _showHidePopup ?? (_showHidePopup = new RelayCommand(
                    () =>
                    {
                        IsSelectConfigurationPopupOpen = IsSelectConfigurationPopupOpen == false;
                    },
                    () =>
                    {
                        return _userSessionService.IsUserLogIn();
                    }));
            }
        }

        public RelayCommand ReportProblemCommand
        {
            get
            {
                return _reportProblemCommand ?? (_reportProblemCommand = new RelayCommand(
                   async () =>
                    {
                        var emailAddress = _currentMobileConfiguration.ReportProblemEmail ??
                                            Model.SfdcConfig.ReportAProblemDefaultEmail;
                        var mailto = new Uri($"mailto:{emailAddress}?subject=Problem found in DSA");
                        await Windows.System.Launcher.LaunchUriAsync(mailto);
                    }));
            }
        }

        public bool IsSelectConfigurationPopupOpen
        {
            get { return _isSelectConfigurationPopupOpen; }
            set { Set(ref _isSelectConfigurationPopupOpen, value); }
        }

        public ObservableCollection<MobileConfigurationsSelectionViewModel> MobileConfigurations
        {
            get { return _mobileConfigurations; }
            set { Set(ref _mobileConfigurations, value); }
        }


        public bool NewContentButtonVisibility
        {
            get { return _newContentButtonVisibility; }
            set { Set(ref _newContentButtonVisibility, value); }
        }

        public RelayCommand NewContentShowDeltaSynchronizationPopupCommand
        {
            get
            {
                return _newContentShowDeltaSynchronizationPopupCommand ?? (_newContentShowDeltaSynchronizationPopupCommand = new RelayCommand(
                    () =>
                    {
                        Messenger.Default.Send(new StartStoryboardMessage { StoryboardName = "NewContentButtonSpin", LoopForever = true });
                        if (ShowDeltaSynchronizationPopup.CanExecute(this))
                        {
                            ShowDeltaSynchronizationPopup.Execute(this);
                        }
                    }));
            }
        }

        public RelayCommand ShowHideAboutCommand
        {
            get
            {
                return _showAboutCommand ?? (_showAboutCommand = new RelayCommand(
                    () =>
                    {
                        IsAboutPopupOpen = IsAboutPopupOpen == false;

                    }));
            }
        }

        private RelayCommand _showMainSettingsFlyoutCommand;
        public RelayCommand ShowMainSettingsFlyoutCommand
        {
            get
            {
                return _showMainSettingsFlyoutCommand ?? (_showMainSettingsFlyoutCommand = new RelayCommand(
                    () =>
                    {
                        var flyout = new MainSettingsFlyout();
                        flyout.ShowIndependent();
                    }
                    ));
            }
        }

        public bool IsAboutPopupOpen
        {
            get { return _isAboutPopupOpen; }
            set { Set(ref _isAboutPopupOpen, value); }
        }

        public ImageSource CheckInOutIcon
        {
            get { return _checkInOutIcon; }
            set { Set(ref _checkInOutIcon, value); }
        }

        public CheckInOutViewModel CheckInOutViewModel
        {
            get { return _checkInOutViewModel; }
            set { Set(ref _checkInOutViewModel, value); }
        }

        public string VersionText => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
    }
}
