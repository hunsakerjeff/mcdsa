/*
 * Copyright (c) 2013, salesforce.com, inc.
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
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Foundation.Metadata;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Auth;


namespace Salesforce.SDK.Rest
{
    /// <summary>
    ///     Factory class for RestClient
    ///     Credentials are stored via the AccountManager
    ///     When no account is found, it kicks off the login flow and creates a new Account
    /// </summary>
    public class ClientManager
    {
        public ClientManager()
        {
            // Create the lcient based http client that will be used by all rest calls
            CreateHttpClient();
        }

        // Attributes
//        private readonly HttpClient _httpClient;        // Note:  this is added here as it is a session object and should be reused as opposed to instantiated over and over
        private static HttpClient _httpClient = null;

        // Properties
        public HttpClient HttpClient { get { return _httpClient; }  }

        /// <summary>
        ///     Logs currently authenticated user out by deleting locally persisted credentials and invoking the server to revoke
        ///     the user auth tokens
        /// </summary>
        /// <returns>true if server logout was successful</returns>
        public async Task<bool> Logout()
        {
            Account account = AccountManager.GetAccount();
            if (account != null)
            {
                LoginOptions options = account.GetLoginOptions();
                AccountManager.DeleteAccount();
                OAuth2.ClearCookies(options);
                bool loggedOut = await OAuth2.RevokeAuthToken(_httpClient, options, account.RefreshToken);
                if (loggedOut)
                {
                    GetRestClient();
                }
                return loggedOut;
            }
            GetRestClient();
            return await Task.Factory.StartNew(() => true);
        }

        /// <summary>
        ///     Returns a RestClient if user is already authenticated or null
        /// </summary>
        /// <returns></returns>
        public RestClient PeekRestClient(Account account = null)
        {
            if (account == null)
            {
                account = AccountManager.GetAccount();
            }
            if (account != null)
            {
                return new RestClient(_httpClient, account.InstanceUrl, account.AccessToken,
                    async () =>
                    {
                        account = AccountManager.GetAccount();
                        AuthResponse authResponse = await OAuth2.RefreshAuthTokenRequest(_httpClient, account.GetLoginOptions(), account.RefreshToken);
                        account.AccessToken = authResponse.AccessToken;
                        AuthStorageHelper.GetAuthStorageHelper().PersistCredentials(account);
                        return account.AccessToken;
                    }
                    );
            }
            System.Diagnostics.Debug.WriteLine("PeekRestClient could not get RestClient");
            return null;
        }

        /// <summary>
        ///     Returns a RestClient if user is already authenticated or otherwise kicks off a login flow
        /// </summary>
        /// <returns></returns>
        public RestClient GetRestClient()
        {
            RestClient restClient = PeekRestClient();
            if (restClient == null)
            {
                PlatformAdapter.Resolve<IAuthHelper>().StartLoginFlow();
            }
            return restClient;
        }

        public RestClient GetUnAuthenticatedRestClient(string instanceUrl)
        {
            return new RestClient(instanceUrl);
        }

        public void Dispose()
        {
            // this does not work like people think.  HttpClient is reentrant and a session object, thus you do not dispose of it.  Its IDisposable does not work that way
            if (_httpClient != null)
            {
                try
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
                catch (Exception)
                {
                    PlatformAdapter.SendToCustomLogger("HttpCall.Dispose - Error occurred while disposing", LoggingLevel.Warning);
                }
            }
        }

        private void CreateHttpClient()
        {
            if (_httpClient == null)
            {
                // Create the lcient based http client that will be used by all rest calls
                var httpBaseFilter = new HttpBaseProtocolFilter
                {
                    AllowUI = false,
                    AllowAutoRedirect = true,
                    AutomaticDecompression = true,
                };
                httpBaseFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
                httpBaseFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
                _httpClient = new HttpClient(httpBaseFilter);
            }
        }
    }
}