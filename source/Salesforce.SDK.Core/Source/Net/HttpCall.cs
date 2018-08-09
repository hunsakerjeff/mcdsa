﻿/*
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using Newtonsoft.Json;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Utilities;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Exceptions;

namespace Salesforce.SDK.Net
{
    /// <summary>
    ///     Enumeration used to represent the content type of a HTTP request
    /// </summary>
    public enum ContentTypeValues
    {
        FormUrlEncoded,
        Json,
        Xml,
        None
    }

    public class HttpCallHeaders
    {
        public HttpCallHeaders(string authorization, Dictionary<string, string> headers)
        {
            if (!String.IsNullOrWhiteSpace(authorization))
            {
                Authorization = new HttpCredentialsHeaderValue("Bearer", authorization);
            }
            Headers = headers;
        }

        public HttpCredentialsHeaderValue Authorization { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
    }

    /// <summary>
    ///     Extension for ContentType enum (to get the mime type of a given content type)
    /// </summary>
    public static class Extensions
    {
        public static string MimeType(this ContentTypeValues contentType)
        {
            switch (contentType)
            {
                case ContentTypeValues.Json:
                    return "application/json";
                case ContentTypeValues.FormUrlEncoded:
                    return "application/x-www-form-urlencoded";
                case ContentTypeValues.Xml:
                    return "text/xml";
                default:
                    return null;
            }
        }
    }

    /// <summary>
    ///     A portable class to send HTTP requests
    ///     HttpCall objects can only be used once
    /// </summary>
    public sealed class HttpCall : IDisposable
    {
        private const string UserAgentHeaderFormat = "SalesforceMobileSDK/3.1 ({0}/{1} {2}) {3}";
        private readonly ContentTypeValues _contentType;
        private readonly HttpCallHeaders _headers;
        private readonly HttpMethod _method;
        private readonly string _requestBody;
        private readonly string _url;
        private readonly HttpClient _webClient;
        private Exception _httpCallErrorException;
        private string _responseBodyText;
        private HttpStatusCode _statusCodeValue;

        /// <summary>
        ///     Constructor for HttpCall
        /// </summary>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <param name="contentType"></param>
        public HttpCall(HttpMethod method, HttpCallHeaders headers, string url, string requestBody,
            ContentTypeValues contentType)
        {
            var httpBaseFilter = new HttpBaseProtocolFilter
            {
                AllowUI = false,
                AllowAutoRedirect = true,
                AutomaticDecompression = true
            };
            httpBaseFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            httpBaseFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            _webClient = new HttpClient(httpBaseFilter);
            _method = method;
            _headers = headers;
            _url = url;
            _requestBody = requestBody;
            _contentType = contentType;
        }

        /// <summary>
        ///     Use this property to retrieve the user agent.
        /// </summary>
        public static string UserAgentHeader { private set; get; }

        /// <summary>
        ///     True if HTTP request has been executed
        /// </summary>
        public bool Executed
        {
            get { return (_responseBodyText != null || _httpCallErrorException != null); }
        }

        /// <summary>
        ///     True if HTTP request was successfully executed
        /// </summary>
        public bool Success
        {
            get
            {
                CheckExecuted();
                return _httpCallErrorException == null;
            }
        }

        /// <summary>
        ///     Error that was raised if HTTP request did not execute successfully
        /// </summary>
        public Exception Error
        {
            get
            {
                CheckExecuted();
                return _httpCallErrorException;
            }
        }

        /// <summary>
        ///     True if the HTTP response returned by the server had a body
        /// </summary>
        public bool HasResponse
        {
            get { return _responseBodyText != null; }
        }

        /// <summary>
        ///     Body of the HTTP response returned by the server
        /// </summary>
        public string ResponseBody
        {
            get
            {
                CheckExecuted();
                return _responseBodyText;
            }
        }

        /// <summary>
        ///     HTTP status code fo the response returned by the server
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get
            {
                CheckExecuted();
                return _statusCodeValue;
            }
        }

        private void CheckExecuted()
        {
            if (!Executed)
            {
                throw new InvalidOperationException("HttpCall must be executed first");
            }
        }

        /// <summary>
        ///     Factory method to build a HttpCall objet for a GET request with additional HTTP request headers
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HttpCall CreateGet(HttpCallHeaders headers, string url)
        {
            return new HttpCall(HttpMethod.Get, headers, url, null, ContentTypeValues.None);
        }

        /// <summary>
        ///     Factory method to build a HttpCall object for a GET request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HttpCall CreateGet(string url)
        {
            return CreateGet(null, url);
        }

        /// <summary>
        ///     Factory method to build a HttpCall object for a POST request with a specific content type
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static HttpCall CreatePost(HttpCallHeaders headers, string url, string requestBody,
            ContentTypeValues contentType)
        {
            return new HttpCall(HttpMethod.Post, headers, url, requestBody, contentType);
        }

        /// <summary>
        ///     Factory method to build a HttpCall object for a POST request with form url encoded arguments
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public static HttpCall CreatePost(string url, string requestBody)
        {
            return CreatePost(null, url, requestBody, ContentTypeValues.FormUrlEncoded);
        }

        /// <summary>
        ///     Async method to execute the HTTP request which expects the HTTP response body to be a Json object that can be
        ///     deserizalized as an instance of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> ExecuteAndDeserialize<T>()
        {
            HttpCall call = await Execute().ConfigureAwait(false);
            if (call.Success)
            {
                return JsonConvert.DeserializeObject<T>(call.ResponseBody);
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(call.ResponseBody))//try to handle refresh token expired
                {
                    JObject error = JObject.Parse(call.ResponseBody);
                    var errorType = error.Property("error");
                    if (errorType != null && errorType.Value.ToString() == "invalid_grant") //refresh token has expired
                    {
                        var errorMessage = error.Property("error_description");
                        throw new OAuthException("invalid_grant", call.Error);
                    }
                }
                //TODO: throws exception that goes up and is not caught
                throw call.Error;
            }
        }

        /// <summary>
        ///     Executes the HttpCall. This will generate the headers, create the request and populate the HttpCall properties with
        ///     relevant data.
        ///     The HttpCall may only be called once; further attempts to execute the same call will throw an
        ///     InvalidOperationException.
        /// </summary>
        /// <returns>HttpCall with populated data</returns>
        public async Task<HttpCall> Execute(CancellationToken token = default(CancellationToken))
        {
            if (Executed)
            {
                throw new InvalidOperationException("A HttpCall can only be executed once");
            }
            var req = new HttpRequestMessage(_method, new Uri(_url));
            // Setting header
            if (_headers != null)
            {
                if (_headers.Authorization != null)
                {
                    req.Headers.Authorization = _headers.Authorization;
                }
                foreach (var item in _headers.Headers)
                {
                    req.Headers[item.Key] = item.Value;
                }
            }
            // if the user agent has not yet been set, set it; we want to make sure this only really happens once since it requires an action that goes to the core thread.
            if (String.IsNullOrWhiteSpace(UserAgentHeader))
            {
                var task = new TaskCompletionSource<string>();
                await Task.Run(async () =>
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, async () =>
                        {
                            await GenerateUserAgentHeader();
                            task.SetResult(UserAgentHeader);
                        });
                }, token);
                await task.Task;
            }
            req.Headers.UserAgent.TryParseAdd(UserAgentHeader);
            if (!String.IsNullOrWhiteSpace(_requestBody))
            {
                switch (_contentType)
                {
                    case ContentTypeValues.FormUrlEncoded:
                        req.Content = new HttpFormUrlEncodedContent(_requestBody.ParseQueryString());
                        break;
                    default:
                        req.Content = new HttpStringContent(_requestBody);
                        req.Content.Headers.ContentType = new HttpMediaTypeHeaderValue(_contentType.MimeType());
                        break;
                }
            }

            HttpResponseMessage message;
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            try
            {
                message = await _webClient.SendRequestAsync(req);
#if DEBUG
                await SaveHttpResponseToFileDebug(message);
#endif
                HandleMessageResponse(message);
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger("HttpClient.Execute threw an exception", LoggingLevel.Error);
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                _httpCallErrorException = ex;
                message = null;
            }
            return this;
        }
        
        private async Task SaveHttpResponseToFileDebug(HttpResponseMessage message)
        {
            var extension = message.Content.Headers.ContentType?.MediaType == "application/json"
                                ? ".bin"
                                : ".json";

            StorageFile newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("req_" + Guid.NewGuid().ToString() + extension);

            var path = newFile.Path;

            using (var sfw = await newFile.OpenStreamForWriteAsync())
            {
                var outStream = sfw.AsOutputStream();
                await message.Content.WriteToStreamAsync(outStream);
            }
        }

        private async void HandleMessageResponse(HttpResponseMessage response)
        {
            // End the operation
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _httpCallErrorException = ex;
                PlatformAdapter.SendToCustomLogger("HttpClient.HandleMessageResponse - EnsureSuccessStatusCode IsSuccessStatusCode is false", LoggingLevel.Error);
            }

            if (response != null)
            {
                
                _responseBodyText = await response.Content.ReadAsStringAsync();
                _statusCodeValue = response.StatusCode;
                response.Dispose();
            }
        }

        private void HandleMessageResponseForBinary(HttpResponseMessage response)
        {
            // End the operation
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _httpCallErrorException = ex;
                PlatformAdapter.SendToCustomLogger("HttpClient.HandleMessageResponseForBinary - EnsureSuccessStatusCode IsSuccessStatusCode is false", LoggingLevel.Error);
            }

            if (response != null)
            {

                _responseBodyText = response.RequestMessage.RequestUri.ToString();
                _statusCodeValue = response.StatusCode;
                response.Dispose();
            }
        }

        public async Task<HttpCall> ExecuteAndSaveAsync(IHttpDataWriter writer, CancellationToken token = default(CancellationToken))
        {
            if (Executed)
            {
                throw new InvalidOperationException("A HttpCall can only be executed once");
            }
            var req = new HttpRequestMessage(_method, new Uri(_url));
            // Setting header
            if (_headers != null)
            {
                if (_headers.Authorization != null)
                {
                    req.Headers.Authorization = _headers.Authorization;
                }
                foreach (var item in _headers.Headers)
                {
                    req.Headers[item.Key] = item.Value;
                }
            }
            // if the user agent has not yet been set, set it; we want to make sure this only really happens once since it requires an action that goes to the core thread.
            if (String.IsNullOrWhiteSpace(UserAgentHeader))
            {
                var task = new TaskCompletionSource<string>();
                await Task.Run(async () =>
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, async () =>
                        {
                            await GenerateUserAgentHeader();
                            task.SetResult(UserAgentHeader);
                        });
                }, token);
                await task.Task;
            }

            req.Headers.UserAgent.TryParseAdd(UserAgentHeader);
            if (!String.IsNullOrWhiteSpace(_requestBody))
            {
                switch (_contentType)
                {
                    case ContentTypeValues.FormUrlEncoded:
                        req.Content = new HttpFormUrlEncodedContent(_requestBody.ParseQueryString());
                        break;
                    default:
                        req.Content = new HttpStringContent(_requestBody);
                        req.Content.Headers.ContentType = new HttpMediaTypeHeaderValue(_contentType.MimeType());
                        break;
                }
            }

            HttpResponseMessage message;
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();    
            }

            try
            {
                message = await _webClient.SendRequestAsync(req, HttpCompletionOption.ResponseHeadersRead).AsTask(token);
                await writer.PushToStreamAsync(message.Content.WriteToStreamAsync, message.Content);
                HandleMessageResponseForBinary(message);
            }
            catch (OperationCanceledException oce)
            {
                PlatformAdapter.SendToCustomLogger("HttpClient.ExecuteAndSaveAsync - Operation Cancelled", LoggingLevel.Error);
                PlatformAdapter.SendToCustomLogger(oce, LoggingLevel.Error);
                throw;
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger("HttpClient.ExecuteAndSaveAsync threw an exception", LoggingLevel.Error);
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                _httpCallErrorException = ex;
                message = null;
            }

            return this;
        }


        /// <summary>
        ///     There is no easy way to retrieve the displayName of an application in a PCL.  This method will retrieve it through
        ///     the application title set by the consumer. If this fails we return the package.id.name instead, allowing the app to still be identified.
        /// </summary>
        /// <returns></returns>
        private static async Task<string> GetApplicationDisplayNameAsync()
        {
            string displayName = String.Empty;

            try
            {
                var config = SDKManager.ServerConfiguration;
                if (config == null)
                {
                    throw new Exception();
                }
                if (!String.IsNullOrWhiteSpace(config.ApplicationTitle))
                {
                    displayName = config.ApplicationTitle;
                }
                else
                {
                    //If no Application title is passed from the consumer of the SDK, fall back to display name from app manifest.
                    StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("AppxManifest.xml");
                    string manifestXml = await FileIO.ReadTextAsync(file);
                    XDocument doc = XDocument.Parse(manifestXml);
                    XNamespace packageNamespace = "http://schemas.microsoft.com/appx/2010/manifest";
                    displayName = (from name in doc.Descendants(packageNamespace + "DisplayName")
                    select name.Value).First();
                }
            }
            catch (Exception)
            {
                //If ApplicationTitle and Display Name both fail, fall back to Package Id
                Debug.WriteLine("Error retrieving application name; using package id name instead");
                displayName = Package.Current.Id.Name;
            }
            return displayName;
        }

        /// <summary>
        ///     This method generates the user agent string for the current device.
        ///     This method can take up to 10 seconds to generate the UserAgent; if it takes longer than 10 seconds the
        ///     UserAgentHeader will not be set.
        /// </summary>
        /// <returns></returns>
        private static async Task<string> GenerateUserAgentHeader()
        {
            var pageCompleted = new TaskCompletionSource<string>();
            var webView = new WebView();
            NotifyEventHandler scriptHandler = null;
            TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> loadHandler = null;
            scriptHandler = async (s, e) =>
            {
                try
                {
                    PackageVersion packageVersion = Package.Current.Id.Version;
                    string packageVersionString = packageVersion.Major + "." + packageVersion.Minor + "." +
                                                  packageVersion.Build;
                    UserAgentHeader = String.Format(UserAgentHeaderFormat, await GetApplicationDisplayNameAsync(),
                    packageVersionString, "native", e.Value);
                    pageCompleted.TrySetResult(UserAgentHeader);
                }
                catch (Exception ex)
                {
                    pageCompleted.SetException(ex);
                }
                finally
                {
                    /* release */
                    webView.ScriptNotify -= scriptHandler;
                    webView.NavigationCompleted -= loadHandler;
                }
            };
            loadHandler = async (web, e) =>
            {
                WebView view = web;
                if (view != null)
                {
                    await view.InvokeScriptAsync("eval", new[] {"window.external.notify(navigator.userAgent); "});
                }
            };
            DateTime endTime = DateTime.Now.AddSeconds(10);
            webView.ScriptNotify += scriptHandler;
            webView.NavigationCompleted += loadHandler;
            webView.NavigateToString("<html />");
            while (pageCompleted.Task.Status != TaskStatus.RanToCompletion)
            {
                await Task.Delay(10);
                if (DateTime.Now >= endTime)
                {
                    pageCompleted.TrySetResult("failed to create userAgent");
                }
            }
            return UserAgentHeader;
        }

        public void Dispose()
        {
            if (_webClient != null)
            {
                try
                {
                    _webClient.Dispose();
                }
                catch (Exception)
                {
                    PlatformAdapter.SendToCustomLogger("HttpCall.Dispose - Error occurred while disposing", LoggingLevel.Warning);
                }
            }
        }
    }
}