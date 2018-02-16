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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Net;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;
using Salesforce.SDK.Utilities;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Adaptation;
using Windows.Foundation.Diagnostics;

namespace Salesforce.SDK.SmartSync.Manager
{
    public class SyncManager
    {
        public const int PageSize = 2000;
        public const string Local = "__local__";
        public const string LocallyCreated = "__locally_created__";
        public const string LocallyUpdated = "__locally_updated__";
        public const string LocallyDeleted = "__locally_deleted__";

        private static volatile Dictionary<string, SyncManager> _instances;
        private static readonly object Synclock = new Object();
        public readonly string ApiVersion;
        public readonly RestClient RestClient;
        private readonly SmartStore.Store.SmartStore _smartStore;

        /// <summary>
        ///     Private constructor 
        /// </summary>
        /// <param name="smartStore"></param>
        /// <param name="client"></param>
        private SyncManager(SmartStore.Store.SmartStore smartStore, RestClient client)
        {
            ApiVersion = ApiVersionStrings.VersionNumber;
            _smartStore = smartStore;
            RestClient = client;
            SyncState.SetupSyncsSoupIfNeeded(smartStore);
        }

        /// <summary>
        ///     Returns the instance of this class associated with current user.
        /// </summary>
        /// <returns> Sync Manager</returns>
        public static SyncManager GetInstance()
        {
            return GetInstance(null);
        }

        /// <summary>
        ///     Returns the instance of this class associated with this user and community.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="communityId"></param>
        /// <returns></returns>
        public static SyncManager GetInstance(Account account, string communityId = null)
        {
            return GetInstance(account, communityId, null);
        }

        public static SyncManager GetInstance(Account account, string communityId, SmartStore.Store.SmartStore smartStore)
        {
            if (account == null)
            {
                account = AccountManager.GetAccount();
            }

            if (smartStore == null)
            {
                smartStore = SmartStore.Store.SmartStore.GetSmartStore(account);
            }

            string uniqueId = Constants.GenerateUniqueId(account, smartStore);
            lock (Synclock)
            {
                var client = SDKManager.GlobalClientManager.PeekRestClient(account);
                SyncManager instance = null;
                if (_instances != null)
                {
                    if (_instances.TryGetValue(uniqueId, out instance))
                    {
                        SyncState.SetupSyncsSoupIfNeeded(instance._smartStore);
                        instance = new SyncManager(instance._smartStore, client);
                        return instance;
                    }

                    instance = new SyncManager(smartStore, client);
                    _instances.Add(uniqueId, instance);
                }
                else
                {
                    _instances = new Dictionary<string, SyncManager>();
                    instance = new SyncManager(smartStore, client);
                    _instances.Add(uniqueId, instance);
                }
                SyncState.SetupSyncsSoupIfNeeded(instance._smartStore);
                return instance;
            }
        }

        /// <summary>
        ///     Resets the Sync manager.
        /// </summary>
        public static void Reset()
        {
            _instances.Clear();
        }

        /// <summary>
        ///     Get details of a sync state.
        /// </summary>
        /// <param name="syncId"></param>
        /// <returns></returns>
        public SyncState GetSyncStatus(long syncId)
        {
            return SyncState.ById(_smartStore, syncId);
        }

        /// <summary>
        ///     Create and run a sync down.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="soupName"></param>
        /// <param name="callback"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<SyncState> SyncDown(SyncDownTarget target, string soupName, Action<SyncState> callback, SyncOptions options = null, CancellationToken token = default(CancellationToken))
        {
            SyncState sync = SyncState.CreateSyncDown(_smartStore, target, soupName, options);
            await RunSync(sync, callback, token);
            return sync;
        }

        public async Task<SyncState> SyncDownAttachment(AttachmentSyncDownTarget target, string soupName, Action<SyncState> callback, SyncOptions options = null, CancellationToken token = default(CancellationToken))
        {
            SyncState sync = SyncState.CreateSyncAttachment(_smartStore, target, soupName, options);
            await RunSync(sync, callback, token);
            return sync;
        }


        /// <summary>
        ///     Create and run a sync up.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="options"></param>
        /// <param name="soupName"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async Task<SyncState> SyncUp(SyncUpTarget target, SyncOptions options, string soupName, Action<SyncState> callback, CancellationToken token = default(CancellationToken), Action<string, string> idChangeHandler = default(Action<string, string>))
        {
            SyncState sync = SyncState.CreateSyncUp(_smartStore, target, options, soupName);
            await RunSync(sync, callback, token, idChangeHandler);
            return sync;
        }

        public async Task<SyncState> ReSync(long syncId, Action<SyncState> callback)
        {
            SyncState sync = SyncState.ById(_smartStore, syncId);
            if (sync == null)
            {
                throw new SmartStoreException("Cannot run ReSync:" + syncId + ": no sync found");
            }
            if (sync.SyncType != SyncState.SyncTypes.SyncDown)
            {
                throw new SmartStoreException("Cannot run ReSync:" + syncId + ": wrong type: " + sync.SyncType);
            }
            var target = (SyncDownTarget)sync.Target;
            if (target.QueryType != SyncDownTarget.QueryTypes.Soql)
            {
                throw new SmartStoreException("Cannot run ReSync:" + syncId + ": wrong query type: " +
                                              target.QueryType);
            }
            if (sync.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SmartStoreException("Cannot run ReSync:" + syncId + ": not done: " + sync.Status);
            }
            await RunSync(sync, callback);
            return sync;
        }

        public async Task RunSync(SyncState sync, Action<SyncState> callback, CancellationToken token = default(CancellationToken), Action<string, string> idChangeHandler = default(Action<string, string>))
        {
            UpdateSync(sync, SyncState.SyncStatusTypes.Running, 0, callback);

            try
            {
                switch (sync.SyncType)
                {
                    case SyncState.SyncTypes.SyncDownAttachment:
                        await SyncDownAttachment(sync, callback, token);
                        break;
                    case SyncState.SyncTypes.SyncDown:
                        await SyncDown(sync, callback, token);
                        break;
                    case SyncState.SyncTypes.SyncUp:
                        await SyncUp(sync, callback, token, idChangeHandler);
                        break;
                }
                UpdateSync(sync, SyncState.SyncStatusTypes.Done, 100, callback);
            }
            catch (OperationCanceledException oce)
            {
                PlatformAdapter.SendToCustomLogger(oce, LoggingLevel.Error);
                UpdateSync(sync, SyncState.SyncStatusTypes.Failed, SyncDownTarget.Unchanged, callback);
                throw;
            }
            catch (ErrorResponseException errorRes)
            {
                PlatformAdapter.SendToCustomLogger(errorRes, LoggingLevel.Error);
                UpdateSync(sync, SyncState.SyncStatusTypes.Failed, SyncDownTarget.Unchanged, callback);
                throw;
            }
            catch (OAuthException oae)
            {
                PlatformAdapter.SendToCustomLogger(oae, LoggingLevel.Error);
                UpdateSync(sync, SyncState.SyncStatusTypes.TokenExpired, SyncDownTarget.Unchanged, callback);
            }
            catch (Exception fail)
            {
                PlatformAdapter.SendToCustomLogger(fail, LoggingLevel.Error);
                UpdateSync(sync, SyncState.SyncStatusTypes.Failed, SyncDownTarget.Unchanged, callback);
            }
        }

        private async Task SyncUp(SyncState sync, Action<SyncState> callback, CancellationToken token = default(CancellationToken), Action<string, string> idChangeHandler = default(Action<string, string>))
        {
            if (sync == null)
                throw new SmartStoreException("SyncState sync was null");
            var target = (SyncUpTarget)sync.Target;

            try
            {
                var account = AccountManager.GetAccount();
                if (account != null)
                {
                    await OAuth2.CallIdentityService(account.IdentityUrl, SyncManager.GetInstance().RestClient);
                }
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                if (!string.IsNullOrEmpty(e.Message) && (e.Message.ToUpper().Contains("FORBIDDEN") || e.Message.Contains("403"))) {
                    throw new OAuthException("Token Expired");
                }
            }

            HashSet<string> dirtyRecordIds = GetDirtyRecordIds(sync.SoupName, SmartStore.Store.SmartStore.SoupEntryId);
            int totalSize = dirtyRecordIds.Count;
            sync.TotalSize = totalSize;
            int i = 0;
            foreach (
                JObject record in
                    dirtyRecordIds.Select(
                        id => _smartStore.Retrieve(sync.SoupName, long.Parse(id))[0].ToObject<JObject>()))
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();    
                }

                List<string> fields = new List<string>( sync.Options.FieldList );
                if (record.ExtractValue<bool>(LocallyDeleted)) { }
                else if (record.ExtractValue<bool>(LocallyCreated))
                {
                    
                }
                else if (record.ExtractValue<bool>(LocallyUpdated))
                {
                    foreach (string value in sync.Options.fieldsToExcludeOnUpdate)
                        if(fields.Contains(value))
                        {
                            fields.Remove(value);
                        }
                }
                bool res = await SyncUpOneRecord(target, sync.SoupName, fields, record, sync.MergeMode, idChangeHandler);
                if (res)
                {
                    i++;
                }
                int progress = i * 100 / totalSize;
                if (progress < 100)
                {
                    UpdateSync(sync, SyncState.SyncStatusTypes.Running, progress, callback);
                }
            }
            if (i<totalSize)
            {
                throw new SmartStoreException("Cannot sync all records");
            }
        }

        private async Task<bool> IsNewerThanServer(SyncUpTarget target, String objectType, String objectId, String lastModStr)
        {
            if (lastModStr == null)
            {
                // We didn't capture the last modified date so we can't really enforce merge mode, returning true so that we will behave like an "overwrite" merge mode
                return true;
            }

            try
            {
                String serverLastModStr = await target.FetchLastModifiedDate(this, objectType, objectId);
                var lastModifiedDate = Int64.Parse(lastModStr);
                if (serverLastModStr != null)
                {
                    var serverLastModifiedDate = Convert.ToDateTime(serverLastModStr).Ticks;
                    return (serverLastModifiedDate <= lastModifiedDate);
                }
            }
            catch (Exception e)
            {
                throw new SmartStoreException(e.Message);
            }
            return false;
        }

        private async Task<bool> SyncUpOneRecord(SyncUpTarget target, string soupName, List<string> fieldList, JObject record,
            SyncState.MergeModeOptions mergeMode, Action<string, string> idChangeHandler = default(Action<string, string>))
        {
            var action = SyncAction.None;
            if (record.ExtractValue<bool>(LocallyDeleted))
                action = SyncAction.Delete;
            else if (record.ExtractValue<bool>(LocallyCreated))
                action = SyncAction.Create;
            else if (record.ExtractValue<bool>(LocallyUpdated))
                action = SyncAction.Update;
            if (SyncAction.None == action)
            {
                // nothing to do for this record
                return true;
            }

            // getting type and id

            string objectType = SmartStore.Store.SmartStore.Project(record, Constants.SobjectType)?.ToString();
            if (objectType == null)
            {
                objectType = record[Constants.SobjectType]?.ToString();
                if(objectType == null)
                    return false;
            }
            var objectId = record.ExtractValue<string>(target.GetId());
            var lastModifiedDate = record.ExtractValue<DateTime>(target.GetModificationDate()).Ticks;

            /*
             * Check if we're attempting to update a record that has been updated on the server after the client update.
             * If merge mode passed in tells us to leave the record alone, we will do nothing and return here.
             */
            if (SyncState.MergeModeOptions.LeaveIfChanged == mergeMode &&
                (action == SyncAction.Update || action == SyncAction.Delete))
            {
                bool isNewer = await IsNewerThanServer(target, objectType, objectId, lastModifiedDate.ToString());
                if (!isNewer) return true;
            }

            var fields = new Dictionary<string, object>();
            if (SyncAction.Create == action || SyncAction.Update == action)
            {
                foreach (
                    string fieldName in
                        fieldList.Where(fieldName => !fieldName.Equals(target.GetId(), StringComparison.CurrentCulture) &&
                            !fieldName.Equals(target.GetModificationDate(), StringComparison.CurrentCulture) &&
                            !Constants.SystemModstamp.Equals(fieldName, StringComparison.CurrentCulture)))
                {
                    if (!record[fieldName].IsNullOrEmpty())
                        fields.Add(fieldName, record[fieldName]);
                }
            }

            switch (action)
            {
                case SyncAction.Create:
                    String recordServerId = await target.CreateOnServerAsync(this, objectType, fields);
                    if (recordServerId != null)
                    {
                        var targetId = target.GetId();
                        if (!string.IsNullOrWhiteSpace(targetId))
                        {
                            string previousId = record[targetId].ToString();
                            record[targetId] = recordServerId;
                            CleanAndSaveRecord(soupName, record);
                            idChangeHandler?.Invoke(previousId, recordServerId); 
                        }
                        if (target.DeleteLocalRecordAfterSave)
                        {
                            _smartStore.Delete(soupName,
                                new[] { record.ExtractValue<long>(SmartStore.Store.SmartStore.SoupEntryId) }, false);
                        }
                    }
                    else
                    {
                        //@todo: error handling
                        return false;
                    }
                    break;
                case SyncAction.Delete:
                    if (await target.DeleteOnServer(this, objectType, objectId))
                    {
                        _smartStore.Delete(soupName,
                        new[] { record.ExtractValue<long>(SmartStore.Store.SmartStore.SoupEntryId) }, false);
                    }
                    else
                    {
                        //@todo: error handling
                        return false;
                    }
                    break;
                case SyncAction.Update:
                    if (await target.UpdateOnServer(this, objectType, objectId, fields))
                    {
                        CleanAndSaveRecord(soupName, record);
                    }
                    else
                    {
                        //@todo: error handling
                        return false;
                    }
                    break;
            }

            return true;
        }


        private void CleanAndSaveRecord(String soupName, JObject record)
        {
            record[Local] = false;
            record[LocallyCreated] = false;
            record[LocallyUpdated] = false;
            record[LocallyDeleted] = false;
            _smartStore.Update(soupName, record,
                        record.ExtractValue<long>(SmartStore.Store.SmartStore.SoupEntryId), false);
        }

        private async Task<bool> SyncDown(SyncState sync, Action<SyncState> callback, CancellationToken token = default(CancellationToken))
        {
            var target = (SyncDownTarget)sync.Target;
            long maxTimeStamp = sync.MaxTimeStamp;
            JArray records = await target.StartFetch(this, sync.MaxTimeStamp);
            if (target.TokenExpired)
            {
                throw new OAuthException("Token Expired");
            }

            int countSaved = 0;
            int totalSize = target.TotalSize;
            sync.TotalSize = totalSize;
            UpdateSync(sync, SyncState.SyncStatusTypes.Running, 0, callback);
            while (records != null && records.Count >0)
            {
                //BEGIN FIX UI freeze on saving 2000 records
                //SaveRecordsToSmartStore(sync.SoupName, records, sync.MergeMode);
                await Task.Run(() => SaveRecordsToSmartStore(sync.SoupName, records, sync.MergeMode), token);
                //END FIX
                countSaved += records.Count;
                maxTimeStamp = Math.Max(maxTimeStamp, target.GetMaxTimeStamp(records));
                if (countSaved < totalSize)
                {
                    UpdateSync(sync, SyncState.SyncStatusTypes.Running, countSaved * 100 / totalSize, callback);
                }
                records = await target.ContinueFetch(this); // <- dodać target.FetchAttachment i wywołać go w linii 417
            }
            sync.MaxTimeStamp = maxTimeStamp;
            return true;
        }

        private async Task<bool> SyncDownAttachment(SyncState sync, Action<SyncState> callback, CancellationToken token = default(CancellationToken))
        {
            var target = (AttachmentSyncDownTarget)sync.Target;
            long maxTimeStamp = sync.MaxTimeStamp;
            JArray records = await target.StartFetch(this, sync.MaxTimeStamp);

            await target.FetchAttachment(this);

            int countSaved = 0;
            int totalSize = target.TotalSize;
            sync.TotalSize = totalSize;
            UpdateSync(sync, SyncState.SyncStatusTypes.Running, 0, callback);
            while (records != null && records.Count > 0)
            {
                //BEGIN FIX UI freeze on saving 2000 records
                //SaveRecordsToSmartStore(sync.SoupName, records, sync.MergeMode);
                await Task.Run(() => SaveRecordsToSmartStore(sync.SoupName, records, sync.MergeMode), token);
                //END FIX
                countSaved += records.Count;
                maxTimeStamp = Math.Max(maxTimeStamp, target.GetMaxTimeStamp(records));
                if (countSaved < totalSize)
                {
                    UpdateSync(sync, SyncState.SyncStatusTypes.Running, countSaved * 100 / totalSize, callback);
                }
                records = await target.ContinueFetch(this);
            }
            sync.MaxTimeStamp = maxTimeStamp;
            return true;
        }


        internal HashSet<string> GetDirtyRecordIds(string soupName, string idField)
        {
            var idsToSkip = new HashSet<string>();
            string dirtyRecordsSql = string.Format("SELECT {{{0}:{1}}} FROM {{{2}}} WHERE {{{3}:{4}}} = 'True'",
                soupName,
                idField, soupName, soupName, Local);
            QuerySpec smartQuerySpec = QuerySpec.BuildSmartQuerySpec(dirtyRecordsSql, PageSize);
            bool hasMore = true;
            for (int pageIndex = 0; hasMore; pageIndex++)
            {
                JArray results = _smartStore.Query(smartQuerySpec, pageIndex);
                hasMore = (results.Count == PageSize);
                idsToSkip.UnionWith(ToSet(results));
            }
            return idsToSkip;
        }

        public static List<T> Pluck<T>(IEnumerable<JToken> jArray, string key)
        {
            return jArray.Select(t => t.ToObject<JObject>().Value<T>(key)).ToList();
        }

        private HashSet<string> ToSet(JArray jsonArray)
        {
            var set = new HashSet<String>();
            List<string> list = jsonArray.Select(t => t.ToObject<JArray>()[0].Value<string>()).ToList();
            set.UnionWith(list);
            return set;
        }

        private void SaveRecordsToSmartStore(string soupName, IEnumerable<JToken> records,
            SyncState.MergeModeOptions mergeMode)
        {
            _smartStore.Database.BeginTransaction();
            HashSet<string> idsToSkip = null;

            if (SyncState.MergeModeOptions.LeaveIfChanged == mergeMode)
            {
                idsToSkip = GetDirtyRecordIds(soupName, Constants.Id);
            }

            foreach (JObject record in records.Select(t => t.ToObject<JObject>()))
            {
                // Skip if LeaveIfChanged and id is in dirty list
                if (idsToSkip != null && SyncState.MergeModeOptions.LeaveIfChanged == mergeMode)
                {
                    var id = record.ExtractValue<string>(Constants.Id);
                    if (!String.IsNullOrWhiteSpace(id) && idsToSkip.Contains(id))
                    {
                        continue; // don't write over dirty record
                    }
                }

                // Save
                record[Local] = false;
                record[LocallyCreated] = false;
                record[LocallyUpdated] = false;
                record[LocallyUpdated] = false;
                _smartStore.Upsert(soupName, record, Constants.Id, false);
            }
            _smartStore.Database.CommitTransaction();
        }

        private void UpdateSync(SyncState sync, SyncState.SyncStatusTypes status, int progress,
            Action<SyncState> callback)
        {
            if (sync == null)
                return;
            sync.Status = status;
            if (progress != SyncDownTarget.Unchanged)
            {
                sync.Progress = progress;
            }
            sync.Save(_smartStore);
            if (callback != null)
            {
                callback(sync);
            }
        }

        public async Task<RestResponse> SendRestRequest(RestRequest request, CancellationToken token = default(CancellationToken))
        {
            return await RestClient.SendAsync(request, token);
        }

        public async Task<RestBinaryResponseSaved> SendRestRequestAndSaveBinary(RestRequest request, IHttpDataWriter outStream, CancellationToken token = default(CancellationToken))
        {
            return await RestClient.SendAndSaveAsync(request, outStream, token);
        }

        private long GetMaxTimeStamp(JArray jArray)
        {
            long maxTimeStamp = SyncDownTarget.Unchanged;
            foreach (JToken t in jArray)
            {
                var jObj = t.ToObject<JObject>();
                if (jObj != null)
                {
                    var date = jObj.ExtractValue<DateTime>(Constants.LastModifiedDate);
                    if (date == null)
                    {
                        maxTimeStamp = SyncDownTarget.Unchanged;
                        break;
                    }
                    try
                    {
                        long timeStamp = date.Ticks;
                        maxTimeStamp = Math.Max(timeStamp, maxTimeStamp);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("SmartSync.GetMaxTimeStamp could not parse LastModifiedDate");
                        maxTimeStamp = SyncDownTarget.Unchanged;
                        break;
                    }
                }
            }
            return maxTimeStamp;
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            if (String.IsNullOrWhiteSpace(text) ||
                String.IsNullOrWhiteSpace(search) ||
                String.IsNullOrWhiteSpace(replace))
            {
                return text;
            }
            int pos = text.IndexOf(search, StringComparison.CurrentCulture);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private enum SyncAction
        {
            Create,
            Update,
            Delete,
            None
        }
    }
}