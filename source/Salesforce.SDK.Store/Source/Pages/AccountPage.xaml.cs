﻿/*
 * Copyright (c) 2014, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Security.Authentication.Web;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.App;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Source.Settings;
using Salesforce.SDK.Strings;
using Windows.Foundation.Diagnostics;
using Windows.UI.Core;

namespace Salesforce.SDK.Source.Pages
{
    // TODO: use MVVM pattern

    /// <summary>
    ///     Phone based page for displaying accounts.
    /// </summary>
    public partial class AccountPage : Page
    {
        private const string SingleUserViewState = "SingleUser";
        private const string MultipleUserViewState = "MultipleUser";
        private const string LoggingUserInViewState = "LoggingUserIn";
        private string _currentState;

        public AccountPage()
        {
            InitializeComponent();
        }

        public Account[] Accounts
        {
            get { return AccountManager.GetAccounts().Values.ToArray(); }
        }

        public Account CurrentAccount
        {
            get
            {
                Account account = AccountManager.GetAccount();
                return account;
            }
        }

        public ObservableCollection<ServerSetting> Servers
        {
            get { return SDKManager.ServerConfiguration.ServerList; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetupAccountPage();
        }

        private void SetupAccountPage()
        {
            ResourceLoader loader = ResourceLoader.GetForCurrentView("Salesforce.SDK.Core/Resources");
            SalesforceConfig config = SDKManager.ServerConfiguration;
            bool titleMissing = true;
            if (!String.IsNullOrWhiteSpace(config.ApplicationTitle) && config.IsApplicationTitleVisible)
            {
                ApplicationTitle.Visibility = Visibility.Visible;
                ApplicationTitle.Text = config.ApplicationTitle;
                titleMissing = false;
            }
            else
            {
                ApplicationTitle.Visibility = Visibility.Collapsed;
            }

            if (config.LoginBackgroundLogo != null)
            {
                if (ApplicationLogo.Items != null)
                {
                    ApplicationLogo.Items.Clear();
                    ApplicationLogo.Items.Add(config.LoginBackgroundLogo);
                }
                if (titleMissing)
                {
                    var padding = new Thickness(10, 24, 10, 24);
                    ApplicationLogo.Margin = padding;
                }
            }

            // set background from config
            if (config.LoginBackgroundColor != null)
            {
                var background = new SolidColorBrush((Color) config.LoginBackgroundColor);
                PageRoot.Background = background;
                Background = background;
                // ServerFlyoutPanel.Background = background;
                //  AddServerFlyoutPanel.Background = background;
            }

            // set foreground from config
            if (config.LoginForegroundColor != null)
            {
                var foreground = new SolidColorBrush((Color) config.LoginForegroundColor);
                Foreground = foreground;
                ApplicationTitle.Foreground = foreground;
                LoginToSalesforce.Foreground = foreground;
                LoginToSalesforce.BorderBrush = foreground;
                ChooseConnection.Foreground = foreground;
                ChooseConnection.BorderBrush = foreground;
            }

            if (Accounts == null || Accounts.Length == 0)
            {
                _currentState = SingleUserViewState;
                SetLoginBarVisibility(Visibility.Collapsed);
                VisualStateManager.GoToState(this, SingleUserViewState, true);
            }
            else
            {
                _currentState = MultipleUserViewState;
                SetLoginBarVisibility(Visibility.Visible);
                ListTitle.Text = loader.GetString("select_account");
                VisualStateManager.GoToState(this, MultipleUserViewState, true);
            }
            ListboxServers.ItemsSource = Servers;
            AccountsList.ItemsSource = Accounts;
            ServerFlyout.Opening += ServerFlyout_Opening;
            ServerFlyout.Closed += ServerFlyout_Closed;
            AddServerFlyout.Opened += AddServerFlyout_Opened;
            AddServerFlyout.Closed += AddServerFlyout_Closed;
            AccountsList.SelectionChanged += accountsList_SelectionChanged;
            ListboxServers.SelectedValue = null;
            HostName.PlaceholderText = LocalizedStrings.GetString("name");
            HostAddress.PlaceholderText = LocalizedStrings.GetString("address");
            AddConnection.Visibility = (SDKManager.ServerConfiguration.AllowNewConnections
                ? Visibility.Visible
                : Visibility.Collapsed);
        }


        private async void accountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await AccountManager.SwitchToAccount(AccountsList.SelectedItem as Account);
            SDKManager.ResetClientManager();
            if (SDKManager.GlobalClientManager.PeekRestClient() != null)
            {
                Frame.Navigate(SDKManager.RootApplicationPage);
            }
        }

        private void AddServerFlyout_Opened(object sender, object e)
        {
            if (AddCustomHostBtn.ActualWidth.CompareTo(CancelCustomHostBtn.ActualWidth) < 0)
            {
                AddCustomHostBtn.Width = CancelCustomHostBtn.ActualWidth;
            }
            else if (AddCustomHostBtn.ActualWidth.CompareTo(CancelCustomHostBtn.ActualWidth) > 0)
            {
                CancelCustomHostBtn.Width = AddCustomHostBtn.ActualWidth;
            }
        }

        private void AddServerFlyout_Closed(object sender, object e)
        {
            TryShowFlyout(ServerFlyout, ApplicationLogo);
        }

        private void ServerFlyout_Closed(object sender, object e)
        {
            SetLoginBarVisibility(Visibility.Visible);
        }

        private void ServerFlyout_Opening(object sender, object e)
        {
            SetLoginBarVisibility(Visibility.Collapsed);
        }

        private void ShowServerFlyout(object sender, RoutedEventArgs e)
        {
            if (Servers.Count <= 1 && !SDKManager.ServerConfiguration.AllowNewConnections)
            {
                ListboxServers.SelectedIndex = 0;
                addAccount_Click(sender, e);
            }
            else
            {
                ServerFlyout.Placement = FlyoutPlacementMode.Bottom;
                TryShowFlyout(ServerFlyout, ApplicationLogo);
            }
        }

        private void DisplayErrorDialog(string message)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                MessageContent.Text = message;
                TryShowFlyout(MessageFlyout, ApplicationLogo);
            });
        }

        private async void DoAuthFlow(LoginOptions loginOptions)
        {
            loginOptions.DisplayType = LoginOptions.DefaultStoreDisplayType;
            var loginUri = new Uri(OAuth2.ComputeAuthorizationUrl(loginOptions));
            var callbackUri = new Uri(loginOptions.CallbackUrl);
            OAuth2.ClearCookies(loginOptions);
            WebAuthenticationResult webAuthenticationResult;

            try
            {
                PlatformAdapter.SendToCustomLogger(
                    "AccountPage.DoAuthFlow - calling WebAuthenticationBroker.AuthenticateAsync()", LoggingLevel.Verbose);
                if (loginOptions.UseTwoParamAuthAsyncMethod)
                {
                    webAuthenticationResult =
                    await
                        WebAuthenticationBroker.AuthenticateAsync(loginOptions.BrokerOptions, loginUri);
                }
                else
                {
                    webAuthenticationResult =
                    await
                        WebAuthenticationBroker.AuthenticateAsync(loginOptions.BrokerOptions, loginUri, callbackUri);
                }
            }
            // If a bad URI was passed in the user is shown an error message by the WebAuthenticationBroken, when user
            // taps back arrow we are then thrown a FileNotFoundException, but since user already saw error message we
            // should just swallow that exception
            catch (FileNotFoundException)
            {
                SetupAccountPage();
                return;
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger("AccountPage.StartLoginFlow - Exception occured", LoggingLevel.Critical);
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Critical);

                DisplayErrorDialog(LocalizedStrings.GetString("generic_error"));
                SetupAccountPage();
                return;
            }

            if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var responseUri = new Uri(webAuthenticationResult.ResponseData);
                if (!String.IsNullOrWhiteSpace(responseUri.Query) &&
                    responseUri.Query.IndexOf("error", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    DisplayErrorDialog(LocalizedStrings.GetString("generic_authentication_error"));
                    SetupAccountPage();
                }
                else
                {
                    try
                    {
                        AuthResponse authResponse = OAuth2.ParseFragment(responseUri.Fragment.Substring(1));
                        PlatformAdapter.SendToCustomLogger("AccountPage.DoAuthFlow - calling EndLoginFlow()", LoggingLevel.Verbose);
                        await PlatformAdapter.Resolve<IAuthHelper>().EndLoginFlow(loginOptions, authResponse);
                    }
                    catch (Exception ex)
                    {
                        DisplayErrorDialog($"Login failed: { ex.Message }");
                        SetupAccountPage();
                    }
                    
                }
            }
            else if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.UserCancel)
            {
                SetupAccountPage();
            }
            else
            {
                DisplayErrorDialog(LocalizedStrings.GetString("generic_error"));
                SetupAccountPage();
            }
        }

        private void addConnection_Click(object sender, RoutedEventArgs e)
        {
            HostName.Text = "";
            HostAddress.Text = "";
            TryShowFlyout(AddServerFlyout, ApplicationLogo);
        }

        private void addCustomHostBtn_Click(object sender, RoutedEventArgs e)
        {
            string hname = HostName.Text;
            string haddress = HostAddress.Text;
            if (String.IsNullOrWhiteSpace(haddress))
            {
                return;
            }
            if (String.IsNullOrWhiteSpace(hname))
            {
                hname = haddress;
            }
            var server = new ServerSetting
            {
                ServerHost = haddress,
                ServerName = hname
            };
            SDKManager.ServerConfiguration.AddServer(server);

            TryShowFlyout(ServerFlyout, ApplicationLogo);
        }

        private void cancelCustomHostBtn_Click(object sender, RoutedEventArgs e)
        {
            TryShowFlyout(ServerFlyout, ApplicationLogo);
        }

        private void LoginToGlobal_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListboxServers.Items != null) StartLoginFlow(ListboxServers.Items[0] as ServerSetting);
        }

        private void LoginToUs_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListboxServers.Items != null) StartLoginFlow(ListboxServers.Items[1] as ServerSetting);
        }

        private void LoginToSalesforce_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListboxServers.Items != null) StartLoginFlow(ListboxServers.Items[0] as ServerSetting);
        }

        private void addAccount_Click(object sender, RoutedEventArgs e)
        {
            StartLoginFlow(ListboxServers.SelectedItem as ServerSetting);
        }

        private void StartLoginFlow(ServerSetting server)
        {
            if (server != null)
            {
                VisualStateManager.GoToState(this, LoggingUserInViewState, true);
                SDKManager.ResetClientManager();
                SalesforceConfig config = SDKManager.ServerConfiguration;
                var clientId = config.GetClientIdForServer(server);
                var callbackUrl = config.GetCallbackUrlForServer(server);
                var brokerOptions = config.GetOptionsForServer(server);
                var useTwoParamAuthAsyncMethod = config.GetUseTwoParamAuthAsyncMethodForServer(server);
                var options = new LoginOptions(server.ServerHost, clientId, callbackUrl, config.Scopes, brokerOptions, useTwoParamAuthAsyncMethod);
                SalesforceConfig.LoginOptions = new LoginOptions(server.ServerHost, clientId, callbackUrl, config.Scopes, brokerOptions, useTwoParamAuthAsyncMethod);
                DoAuthFlow(options);
            }
        }

        private void SetLoginBarVisibility(Visibility state)
        {
            LoginBar.Visibility = MultipleUserViewState.Equals(_currentState) ? state : Visibility.Collapsed;
        }

        private void CloseMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageFlyout.Hide();
        }

        private void ClickServer(object sender, RoutedEventArgs e)
        {
            StartLoginFlow(ListboxServers.SelectedItem as ServerSetting);
        }

        private void DeleteServer(object sender, RoutedEventArgs e)
        {
            SDKManager.ServerConfiguration.ServerList.Remove(ListboxServers.SelectedItem as ServerSetting);
            SDKManager.ServerConfiguration.SaveConfig();
        }

        private void TryShowFlyout(Flyout flyout, FrameworkElement location)
        {
            try
            {
                flyout.ShowAt(location);
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("Error displaying flyout");
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }
    }
}