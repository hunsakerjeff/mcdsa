/*
 * Copyright (c) 2015, salesforce.com, inc.
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
using System.Reflection;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Model
{
    /// <summary>
    ///     Target for sync u i.e. set of objects to download from server
    /// </summary>
    public class AttachmentSyncDownTarget : SyncDownTarget
    {
        public const string AttachmentQuery = "SELECT BodyLength,ContentType,Id,LastModifiedDate,ParentId FROM Attachment WHERE id={0}";
        public const string AttachmentBody = "/sobjects/Attachment/{0}/body";

        public const string QueryString = "query";
        public string Query { protected set; get; }
        public string AttachmentBodyUrl { protected set; get; }
          

        public AttachmentSyncDownTarget(string attachmentId) : base(string.Format(AttachmentQuery, attachmentId))
        {
            if (String.IsNullOrWhiteSpace(attachmentId)) { throw new ArgumentException("Parameter attachmentId is required."); }

            AttachmentId = attachmentId;

            Query = string.Format(AttachmentQuery, attachmentId);
            QueryType = QueryTypes.Soql;

            AttachmentBodyUrl = String.Format(AttachmentBody, attachmentId);    
        }

        public bool skipGroupingParenthesis = false;
        public void setSkipGroupingParenthesis(bool skip)
        {
            skipGroupingParenthesis = skip;
        }
        public AttachmentSyncDownTarget(JObject target) : base(target)
        {
            this.Query = target.ExtractValue<string>(QueryString);
        }

        //public string Query { protected set; get; }
        public string NextRecordsUrl { protected set; get; }
        public string AttachmentId { get; private set; }

        /// <summary>
        /// </summary>
        /// <returns>json representation of target</returns>
        public override JObject AsJson()
        {
            var target = base.AsJson();
            if (!String.IsNullOrWhiteSpace(Query)) target[Constants.Query] = Query;
            target["skipGroupingParenthesis"] = skipGroupingParenthesis;
            return target;
        }

        public override async Task<JArray> StartFetch(SyncManager syncManager, long maxTimeStamp)
        {
            string queryToRun = maxTimeStamp > 0 ? AddFilterForReSync(Query, maxTimeStamp, skipGroupingParenthesis) : Query;
            RestRequest request = RestRequest.GetRequestForQuery(syncManager.ApiVersion, queryToRun);
            RestResponse response = await syncManager.SendRestRequest(request);
            if (response.Success == false)
            {
                TotalSize = -1;
                NextRecordsUrl = null;
                return null;
            }
            JObject responseJson = response.AsJObject;
            var records = responseJson.ExtractValue<JArray>(Constants.Records);

            // Record total size
            TotalSize = responseJson.ExtractValue<int>(Constants.TotalSize);

            // Capture next records url
            NextRecordsUrl = responseJson.ExtractValue<string>(Constants.NextRecordsUrl);

            return records;
        }

        public override async Task<JArray> ContinueFetch(SyncManager syncManager)
        {
            if (String.IsNullOrWhiteSpace(NextRecordsUrl))
            {
                return null;
            }

            var request = new RestRequest(HttpMethod.Get, NextRecordsUrl, null);
            RestResponse response = await syncManager.SendRestRequest(request);
            JObject responseJson = response.AsJObject;

            // Capture next records url
            NextRecordsUrl = responseJson.ExtractValue<string>(Constants.NextRecordsUrl);

            var records = responseJson.ExtractValue<JArray>(Constants.Records);
            return records;
        }

        public async Task<bool> FetchAttachment(SyncManager syncManager)
        {
            var request = RestRequest.GetRequestForRetrieve("v31.0", "Attachment", AttachmentId, new string[] { "body" });

            var response = await syncManager.SendRestRequest(request);

            if (response.Success == false)
            {
                TotalSize = -1;
                NextRecordsUrl = null;
                return false;
            }

            JObject responseJson = response.AsJObject;

            return true;
        }

        public static AttachmentSyncDownTarget FromJson(JObject target)
        {
            if (target == null) return null;
            JToken impl;
            if (target.TryGetValue(WindowsImpl, out impl))
            {
                JToken implType;
                if (target.TryGetValue(WindowsImplType, out implType))
                {
                    try
                    {
                        Assembly assembly = Assembly.Load(new AssemblyName(impl.ToObject<string>()));
                        Type type = assembly.GetType(implType.ToObject<string>());
                        if (type.GetTypeInfo().IsSubclassOf(typeof(SyncTarget)))
                        {
                            MethodInfo method = type.GetTypeInfo().GetDeclaredMethod("FromJson");
                            return (AttachmentSyncDownTarget)method.Invoke(type, new object[] { target });
                        }
                    }
                    catch (Exception)
                    {
                        throw new SmartStoreException("Invalid  AttachmentSyncDownTarget");
                    }
                }
            }
            throw new SmartStoreException("Could not generate  AttachmentSyncDownTarget from json target");
        }


    }
}