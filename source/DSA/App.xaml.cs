using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DSA.Model;
using DSA.Model.Messages;
using DSA.Sfdc.Sync;
using DSA.Shell.Pages;
using DSA.Shell.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.App;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Source.Security;
using DSA.Shell.Logging;

namespace DSA.Shell
{
    sealed partial class App
    {
        public App()
        {
            // SfdcSDK
            InitializeSfdcConfig();

            SDKManager.CreateClientManager(false);
            SDKManager.RootApplicationPage = typeof(VisualBrowserPage);
            SDKManager.EndLoginCallBack = () => { Messenger.Default.Send(new UserLogInMessage()); };

            PlatformAdapter.Resolve<ISFApplicationHelper>().Initialize();

            // Set the custom log action function
            LoggingServices ls = LoggingServices.Instance;
            ls.Initialize();
            PlatformAdapter.SetCustomLoggerAction(LoggingServices.LogAction);

            InitializeComponent();
            Suspending += OnSuspending;
            Resuming += OnResuming;

            // setup the global crash handler... (MetroLog)
            //GlobalCrashHandler.Configure();
        }

        /// <summary>
        /// Config initialization
        /// </summary>
        private void InitializeSfdcConfig()
        {
            var config = SDKManager.InitializeConfig<SfdcConfig>(new EncryptionSettings(new HmacSHA256KeyGenerator()));
            config.SaveConfig();

            var initialRefresher = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            initialRefresher.Start();
            initialRefresher.Tick += (o, e) =>
            {
                RefreshToken();
                initialRefresher.Stop();
            };
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        private async void RefreshToken()
        {
            try
            {
                //var nis = SimpleIoc.Default.GetInstance<INetworkInformationService>();
                if (ObjectSyncDispatcher.HasInternetConnection())
                {
                    await ObjectSyncDispatcher.Instance.RefreshToken();
                    //_logger.LogTrace("AppBootstrapper-RefreshToken| Trying to refresh token");
                    //await AuthenticationService.RefreshToken();
                    Debug.WriteLine("App RefreshToken");
                }
            }
            catch (OAuthException oae)
            {
                //_logger.LogException("AppBootstrapper-RefreshToken|", oae);
                Debug.WriteLine(oae);
            }
            catch (Exception ex)
            {
                //_logger.LogException("AppBootstrapper-RefreshToken|", ex);
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                DispatcherHelper.Initialize();
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(VisualBrowserPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            Messenger.Default.Send(new AppResumingMessage());
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void OnResuming(object sender, object e)
        {
            Messenger.Default.Send(new AppResumingMessage());
            RefreshToken();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            ViewModelLocator.AttachSharingService();
            base.OnWindowCreated(args);
        }
    }
}
