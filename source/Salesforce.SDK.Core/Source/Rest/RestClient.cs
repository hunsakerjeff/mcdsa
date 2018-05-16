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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.Web.Http;
using Salesforce.SDK.Net;
using Salesforce.SDK.Utilities;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Adaptation;
using Windows.Foundation.Diagnostics;
using System;

namespace Salesforce.SDK.Rest
{
    public delegate void AsyncRequestCallback(RestResponse response);

    public delegate Task<string> AccessTokenProvider();

    public class RestClient
    {
        // Attributes
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly string _instanceUrl;
        private string _accessToken;
        private readonly HttpClient _httpClient;        // Note:  this is added here as it is a session object and should be reused as opposed to instantiated over and over

        // CTOR
        public RestClient(HttpClient client, string instanceUrl, string accessToken, AccessTokenProvider accessTokenProvider)
        {
            _instanceUrl = instanceUrl;
            _accessToken = accessToken;
            _accessTokenProvider = accessTokenProvider;
            _httpClient = client;
        }

        public RestClient(string instanceUrl)
        {
            _instanceUrl = instanceUrl;
        }

        public string InstanceUrl
        {
            get { return _instanceUrl; }
        }

        public string AccessToken
        {
            get { return _accessToken; }
        }

        public async Task<RestResponse> SendAsync(HttpMethod method, string url)
        {
            return await SendAsync(new RestRequest(method, url));
        }

        public async void SendAsync(RestRequest request, AsyncRequestCallback callback)
        {
            RestResponse result = await SendAsync(request).ConfigureAwait(false);
            if (callback != null)
            {
                callback(result);
            }
        }

        public async Task<RestResponse> SendAsync(RestRequest request, CancellationToken token = default(CancellationToken))
        {
            HttpCall result = await Send(request, true, token);
            return new RestResponse(result);
        }

        private async Task<HttpCall> Send(RestRequest request, bool retryInvalidToken, CancellationToken token = default(CancellationToken))
        {
            string url = _instanceUrl + request.Path;
            var headers = request.AdditionalHeaders != null ? new HttpCallHeaders(_accessToken, request.AdditionalHeaders) : new HttpCallHeaders(_accessToken, new Dictionary<string, string>());

            HttpCall call = await new HttpCall(_httpClient, request.Method, headers, url, request.RequestBody, request.ContentType).Execute(token).ConfigureAwait(false);
            if (!call.HasResponse)
            {
                throw call.Error;
            }

            if (call.StatusCode == HttpStatusCode.Unauthorized || call.StatusCode == HttpStatusCode.Forbidden)
            {
                if (retryInvalidToken && _accessTokenProvider != null)
                {
                    //try catch refresh token expires exception
                    string newAccessToken = null;
                    try
                    {
                        newAccessToken = await _accessTokenProvider();
                    }
                    catch (OAuthException ex)
                    {
                        PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                        return call;
                    }
                    if (newAccessToken != null)
                    {
                        _accessToken = newAccessToken;
                        call = await Send(request, false, token);
                    }
                }
            }

            if (call.Success == false && call.HasResponse)
            {
                throw new ErrorResponseException(call);
            }

            return call;
        }

        public async Task<RestBinaryResponseSaved> SendAndSaveAsync(RestRequest request, IHttpDataWriter outStream, CancellationToken token = default(CancellationToken))
        {
            HttpCall result = await SendAndSaveAsync(request, outStream, true, token);
            return new RestBinaryResponseSaved(result);
        }

        private async Task<HttpCall> SendAndSaveAsync(RestRequest request, IHttpDataWriter outStream, bool retryInvalidToken, CancellationToken token = default(CancellationToken))
        {
            string url = _instanceUrl + request.Path;
            var headers = request.AdditionalHeaders != null ? new HttpCallHeaders(_accessToken, request.AdditionalHeaders) : new HttpCallHeaders(_accessToken, new Dictionary<string, string>());

            HttpCall call = await new HttpCall(_httpClient, request.Method, headers, url, request.RequestBody, request.ContentType).ExecuteAndSaveAsync(outStream, token).ConfigureAwait(false);
            if (!call.HasResponse)
            {
                System.Diagnostics.Debug.WriteLine("***HttpCall !call.HasResponse ");
                PlatformAdapter.SendToCustomLogger("HttpCall: no valid response", LoggingLevel.Error);

                throw new NetworkErrorException();
            }

            if (call.StatusCode == HttpStatusCode.Unauthorized || call.StatusCode == HttpStatusCode.Forbidden)
            {
                if (retryInvalidToken && _accessTokenProvider != null)
                {
                    string newAccessToken = await _accessTokenProvider();
                    if (newAccessToken != null)
                    {
                        _accessToken = newAccessToken;
                        call = await Send(request, false, token);
                    }
                }
            }

            if (call.Success == false && call.HasResponse)
            {
                throw new ErrorResponseException(call);
            }

            return call;
        }
    }
}