using System;
using System.Collections.Generic;
using Windows.Security.Authentication.Web;
using Windows.UI;
using DSA.Model.Enums;
using Salesforce.SDK.Source.Settings;

namespace DSA.Model
{
    /// <summary>
    /// Configuration for app and Mobile SDK
    /// </summary>
    public class SfdcConfig : SalesforceConfig
    {
        private const string MastercardDSACallbackUrl = @"dsa:///mastercard";
        private static readonly string AppCallbackUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();

        private struct OAuthSetting
        {
            internal string ClientId { get; set; }
            internal string CallbackUrl { get; set; }
            internal WebAuthenticationOptions Options { get; set; }
            internal bool UseTwoParamAuthAsyncMethod { get; set; }
        }

        private readonly Dictionary<string, OAuthSetting> _oAuthSettings = new Dictionary<string, OAuthSetting>
        {
            {
                "https://test.salesforce.com",
                new OAuthSetting
                {
                    CallbackUrl = "https://test.salesforce.com/services/oauth2/success",
                    ClientId = "3MVG9yZ.WNe6byQArrGXHfKC8Odebkz46h5_viRgVA6IUviZ4jOZZRWNQds0n_OH0m2y7.hUloTQ836aY9iHA",
                    Options = WebAuthenticationOptions.None,
                    UseTwoParamAuthAsyncMethod = false
                }
            },
            {
                "https://login.salesforce.com",
                new OAuthSetting
                {
                    CallbackUrl = "https://login.salesforce.com/services/oauth2/success",
                    ClientId = "3MVG9yZ.WNe6byQArrGXHfKC8Odebkz46h5_viRgVA6IUviZ4jOZZRWNQds0n_OH0m2y7.hUloTQ836aY9iHA",
                    Options = WebAuthenticationOptions.None,
                    UseTwoParamAuthAsyncMethod = false
                }
            },
            //{
            //    "https://das.my.salesforce.com",
            //    new OAuthSetting
            //    {
            //        CallbackUrl = AppCallbackUri,
            //        ClientId = "3MVG9uudbyLbNPZO8FaeCLakmLYZkB5zRMTilZfI3M_aqXoj6lXhzjCilUohl.GezX4W5C24.qbV50VpCCH_f",
            //        Options = WebAuthenticationOptions.UseCorporateNetwork,
            //        UseTwoParamAuthAsyncMethod = true
            //    }
            //},
            //{
            //    "https://dasus.my.salesforce.com",
            //    new OAuthSetting
            //    {
            //        CallbackUrl = AppCallbackUri,
            //        ClientId = "3MVG9uudbyLbNPZO8FaeCLakmLYZkB5zRMTilZfI3M_aqXoj6lXhzjCilUohl.GezX4W5C24.qbV50VpCCH_f",
            //        Options = WebAuthenticationOptions.UseCorporateNetwork,
            //        UseTwoParamAuthAsyncMethod = true
            //    }
            //},
             // Mastercard settings
             // production
            {
                "https://mastercard.my.salesforce.com",
                new OAuthSetting
                {
                    CallbackUrl = MastercardDSACallbackUrl,
                    ClientId = "3MVG9yZ.WNe6byQArrGXHfKC8Odebkz46h5_viRgVA6IUviZ4jOZZRWNQds0n_OH0m2y7.hUloTQ836aY9iHA",
                    Options = WebAuthenticationOptions.UseHttpPost,
                    UseTwoParamAuthAsyncMethod = false
                }
            }
        };

        // Attributes
        private bool _useAutoSync = true;

        /// <summary>
        /// bool sets whether we use autosync or not which does Delta syncs in the background in the event of new content
        /// </summary>
        public bool UseAutoAsync
        {
            get { return _useAutoSync; }
            set { _useAutoSync = value; }
        }

        public override string ClientId
        {
            get { return "3MVG9yZ.WNe6byQArrGXHfKC8Odebkz46h5_viRgVA6IUviZ4jOZZRWNQds0n_OH0m2y7.hUloTQ836aY9iHA"; }
        }

        /// <summary>
        ///     This should return the callback url generated when you create a connected app through Salesforce.
        ///     Note: Read section 'Connecting with single sign-on (SSO)' https://msdn.microsoft.com/windows/uwp/security/web-authentication-broker
        /// </summary>
        public override string CallbackUrl
        {
            get { return "https://login.salesforce.com/services/oauth2/success"; }
        }

        public override WebAuthenticationOptions Options
        {
            get { return WebAuthenticationOptions.None; }
        }

        public override bool UseTwoParamAuthAsyncMethod
        {
            get { return false; }
        }

        /// <summary>
        ///     Return the scopes that you wish to use in your app. Limit to what you actually need, try to refrain from listing
        ///     all scopes.
        /// </summary>
        public override string[] Scopes
        {
            get { return new[] { "api", "web", "offline_access" }; }
        }

        public override Color? LoginBackgroundColor
        {
            get { return Colors.DeepSkyBlue; }
        }

        public override string ApplicationTitle
        {
            get { return "Salesforce Services Digital Sales Aid"; }
        }

        public override Uri LoginBackgroundLogo
        {
            get { return null; } // TODO: Select asset
        }

        /// <summary>
        /// Set this property to true if you want the Title to show up on start screen of the app.
        /// </summary>
        public override bool IsApplicationTitleVisible
        {
            get { return false; }
        }

        public override string GetClientIdForServer(ServerSetting server)
        {
            return _oAuthSettings.ContainsKey(server.ServerHost) ? _oAuthSettings[server.ServerHost].ClientId : ClientId;
        }

        public override string GetCallbackUrlForServer(ServerSetting server)
        {
            return _oAuthSettings.ContainsKey(server.ServerHost) ? _oAuthSettings[server.ServerHost].CallbackUrl : CallbackUrl;
        }

        public override WebAuthenticationOptions GetOptionsForServer(ServerSetting server)
        {
            return _oAuthSettings.ContainsKey(server.ServerHost) ? _oAuthSettings[server.ServerHost].Options : Options;
        }

        public override bool GetUseTwoParamAuthAsyncMethodForServer(ServerSetting server)
        {
            return _oAuthSettings.ContainsKey(server.ServerHost) ? _oAuthSettings[server.ServerHost].UseTwoParamAuthAsyncMethod : UseTwoParamAuthAsyncMethod;
        }

        /* <summary>
        // Application additional settings
        */

        public const bool EmailOnlyContentDistributionLinks = true;

        public const bool SyncLogsEnable = false;

        public const bool CollectSearchTerms = false;

        public const bool RequireContentReviewRatingsAtCheckout = true;

        public const SearchMode AppSearchMode = SearchMode.UseSearchPage;

        public const int PageSize = 1000;

        public const int ConcurrentDownloadsLimit = 1;

        public const decimal RequiredStorageBufferBytes = 10485760;

        public const string SynchronizationFailedSupportEmail = "support@salesforce.com";

        public const string ReportAProblemDefaultEmail = "support@salesforce.com";

        public const string CustomerPrefix = "MCDSA";
    }
}
