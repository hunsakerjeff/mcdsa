using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSA.Model;
using DSA.Sfdc.Sync;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Net;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;
using Salesforce.SDK.Utilities;

namespace DSA.Sfdc.SObjects.Abstract
{
    public abstract class SObject
    {
        private readonly AsyncLock _mutex = new AsyncLock();

        public string SfdcApiVersion { get; set; } = "v31.0";

        protected SmartStore Store { get; }

        protected Action<string> CallBackHandler;

        protected Action<string, string> IdChangeHandler;

        protected bool ClearSoup = true;

        protected IndexSpec[] IndexSpecs
        {
            get { return NotesIndexSpec.ToArray(); }
        }

        internal virtual List<string> FieldsToSyncUp { get; set; } = new List<string>();

        internal virtual List<string> FieldsToExcludeOnUpdate { get; set; } = new List<string>();
        
        internal virtual string SoqlQuery { get; }

        internal string AttachmentId { get; set; }

        internal string LatestPublishedVersionId { get; set; }

        private string _soupName = string.Empty;

        protected string Prefix { get; }

        protected int PageSize { get; }

        internal virtual bool DeleteAfterSyncUp { get; set; } = false;

        protected string SoupName
        {
            get { return string.IsNullOrWhiteSpace(_soupName) ? this.GetType().Name : _soupName; }
            set { _soupName = value;  }
        }

        internal virtual void LocalIdChangeHandler(string previousId, string newId)
        {

        }

        protected string TempSoupName { get; set; }

        protected List<IndexSpec> NotesIndexSpec = new List<IndexSpec>
        {
            new IndexSpec(SyncManager.LocallyCreated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyUpdated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyDeleted, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.Local, SmartStoreType.SmartString)
        };

        internal SObject(SmartStore store)
        {
            if (store == null) { throw new ArgumentNullException("Parameter store is mandatory"); }
            Store = store;
            Prefix = SfdcConfig.CustomerPrefix;
            PageSize = SfdcConfig.PageSize;
            FieldsToSyncUp = FieldsToSyncUp.Select(field => string.Format(field, Prefix)).ToList();
            FieldsToExcludeOnUpdate = FieldsToExcludeOnUpdate.Select(field => string.Format(field, Prefix)).ToList();
        }

        protected void AddIndexSpecItems(IndexSpec[] items)
        {
            NotesIndexSpec.AddRange(items);
        }

        public virtual async Task<SyncState> SyncUpRecords(Action<string> callbackHandler, CancellationToken token = default(CancellationToken), Action<string, string> idChangeHandler = default(Action<string, string>))
        {
            using (await _mutex.LockAsync(token))
            {
                //public Action<string, string> IdChangeHandler
                CallBackHandler = callbackHandler;

                RegisterSoup();

                var options = SyncOptions.OptionsForSyncUp(FieldsToSyncUp, SyncState.MergeModeOptions.Overwrite);
                options.setFieldsToExcludeOnUpdate(FieldsToExcludeOnUpdate);
                var target = new SyncUpTarget(DeleteAfterSyncUp);


                SyncState syncResult = new SyncState { Status = SyncState.SyncStatusTypes.Failed };

                try
                {
                    var syncManager = SyncManager.GetInstance(AccountManager.GetAccount());
                    syncResult = await syncManager.SyncUp(target, options, SoupName, HandleSyncUpdate, token, LocalIdChangeHandler);
                }
                catch (SmartStoreException sse)
                {
                    CreateLogItem("SmartStoreException", sse);
                }
                catch (OperationCanceledException oce)
                {
                    CreateLogItem("OperationCanceledException", oce);
                    throw;
                }
                catch (Exception e)
                {
                    CreateLogItem("General exception", e);
                }
                return syncResult;
            }
        }

        public virtual async Task<SyncState> SyncDownRecords(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(SoqlQuery))
            {
                return new SyncState { Status = SyncState.SyncStatusTypes.Done };
            }

            CallBackHandler = callbackHandler;

            RegisterClearSoup();

            var options = SyncOptions.OptionsForSyncDown(SyncState.MergeModeOptions.Overwrite);
            var target = new SoqlSyncDownTarget(SoqlQuery);

            if (target.Query == null) //there are no data to sync down, so we return success
            {
                return new SyncState { Status = SyncState.SyncStatusTypes.Done };
            }


            SyncState syncResult = new SyncState { Status =  SyncState.SyncStatusTypes.Failed };
            try
            {
                var syncManager = SyncManager.GetInstance(AccountManager.GetAccount());
                syncResult = await syncManager.SyncDown(target, SoupName, HandleSyncUpdate, options, token);
            }
            catch (SmartStoreException sse)
            {
                CreateLogItem("SmartStoreException", sse);
            }
            catch (OperationCanceledException oce)
            {
                CreateLogItem("OperationCanceledException", oce);
                throw;
            }
            catch (ErrorResponseException errorRes)
            {
                CreateLogItem("ErrorResponseException", errorRes);
                throw;
            }
            catch (Exception e)
            {
                CreateLogItem("General exception", e);
            }

            return syncResult;
        }

        public virtual async Task<SyncState> SyncDownRecordsToSoup(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(SoqlQuery))
            {
                return new SyncState { Status = SyncState.SyncStatusTypes.Done };
            }

            CallBackHandler = callbackHandler;

            if (!string.IsNullOrWhiteSpace(TempSoupName) && !Store.HasSoup(TempSoupName))
            {
                RegisterClearSoupBySoupName(TempSoupName);
            }

            if (!Store.HasSoup(SoupName))
            {
                RegisterClearSoup();
            }

            var options = SyncOptions.OptionsForSyncDown(SyncState.MergeModeOptions.Overwrite);
            var target = new SoqlSyncDownTarget(SoqlQuery);


            SyncState syncResult = new SyncState { Status = SyncState.SyncStatusTypes.Failed };

            try
            {
                var syncManager = SyncManager.GetInstance(AccountManager.GetAccount());
                if (!string.IsNullOrWhiteSpace(TempSoupName))
                {
                    syncResult = await syncManager.SyncDown(target, TempSoupName, HandleSyncUpdate, options, token);
                }
                else
                {
                    syncResult = await syncManager.SyncDown(target, SoupName, HandleSyncUpdate, options, token);
                }
            }
            catch (SmartStoreException sse)
            {
                CreateLogItem("SmartStoreException", sse);
            }
            catch (Exception e)
            {
                CreateLogItem("General exception", e);
            }

            return syncResult;
        }

        public virtual async Task<bool> SyncDownAndSaveAttachment(Action<string> callbackHandler, IHttpDataWriter outStream, CancellationToken token = default(CancellationToken))
        {
            CallBackHandler = callbackHandler;

            if (string.IsNullOrWhiteSpace(AttachmentId))
            {
                throw new ArgumentNullException("AttachmentId in SfdcObject is null/empty.");
            }

            var success = false;

            try
            {
                var syncManager = SyncManager.GetInstance(AccountManager.GetAccount());

                var request = RestRequest.GetRequestForAttachmentDownload(SfdcApiVersion, AttachmentId);

                var binResponse = await syncManager.SendRestRequestAndSaveBinary(request, outStream, token);

                success = binResponse.Success;
            }
            catch (SmartStoreException sse)
            {
                CreateLogItem("SmartStoreException", sse);
            }
            catch (OperationCanceledException oce)
            {
                CreateLogItem("OperationCanceledException", oce);
                throw;
            }
            catch (ErrorResponseException ere)
            {
                CreateLogItem("ErrorResponseException", ere);
                throw;
            }
            catch (NetworkErrorException nee)
            {
                CreateLogItem("NetworkErrorException", nee);
                throw;
            }
            catch (Exception e)
            {
                CreateLogItem("General exception", e);
            }

            return success;
        }

        public virtual async Task<bool> SyncDownAndSaveVersionData(Action<string> callbackHandler, IHttpDataWriter outStream, CancellationToken token = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(LatestPublishedVersionId))
            {
                throw new ArgumentNullException("LatestPublishedVersionId in SfdcObject is null/empty.");
            }

            CallBackHandler = callbackHandler;

            var success = false;

            try
            {
                var syncManager = SyncManager.GetInstance(AccountManager.GetAccount());

                var request = RestRequest.GetRequestForVersionDataDownload(SfdcApiVersion, LatestPublishedVersionId);

                var binResponse = await syncManager.SendRestRequestAndSaveBinary(request, outStream, token);

                success = binResponse.Success;
            }
            catch (SmartStoreException sse)
            {
                CreateLogItem("SmartStoreException", sse);
            }
            catch (OperationCanceledException oce)
            {
                CreateLogItem("OperationCanceledException", oce);
                throw;
            }
            catch (ErrorResponseException ere)
            {
                CreateLogItem("ErrorResponseException", ere);
                throw;
            }
            catch (NetworkErrorException nee)
            {
                CreateLogItem("NetworkErrorException", nee);
                throw;
            }
            catch (Exception e)
            {
                CreateLogItem("General exception", e);
            }

            return success;
        }

        private void HandleSyncUpdate(SyncState sync)
        {
            if (SyncState.SyncStatusTypes.Failed == sync.Status)
            {
                if (CallBackHandler != null)
                {
                    CallBackHandler("sync failed");
                }
                else
                {
                    throw new SyncException("SfdcObject sync failed");
                }

                return;
            }

            if (SyncState.SyncStatusTypes.Done != sync.Status) return;

            switch (sync.SyncType)
            {
                case SyncState.SyncTypes.SyncUp:
                    break;
                case SyncState.SyncTypes.SyncDown:
                    CallBackHandler("sync done");
                    break;
                case SyncState.SyncTypes.SyncDownAttachment:
                    CallBackHandler("attachment sync done");
                    break;
            }
        }

        protected virtual void CreateLogItem(string message, Exception e)
        {

        }

        internal virtual void RegisterSoup()
        {
            if (!Store.HasSoup(SoupName))
            {
                Store.RegisterSoup(SoupName, IndexSpecs);
            }
        }

        internal virtual void RegisterClearSoup()
        {
            if (ClearSoup && Store.HasSoup(SoupName))
            {
                Store.DropSoup(SoupName);
            }
            RegisterSoup();
        }

        internal virtual void RegisterClearSoupBySoupName(string soupName)
        {
            if (Store.HasSoup(soupName))
            {
                Store.DropSoup(soupName);
            }

            Store.RegisterSoup(soupName, IndexSpecs);
        }

        public virtual JArray Query(string smartSql)
        {
            QuerySpec querySpec = QuerySpec.BuildSmartQuerySpec(smartSql, PageSize);

            var count = (int)Store.CountQuery(querySpec);

            querySpec = QuerySpec.BuildSmartQuerySpec(smartSql, count);
            var result = Store.Query(querySpec, 0);
            return result;
        }

        protected bool SaveMetadataToSoup(IList<JToken> results)
        {
            var endWithError = false;
            foreach (var result in results)
            {
                try
                {
                    Store.Database.BeginTransaction();
                    var record = result.ToObject<JObject>();
                    Store.Upsert(SoupName, record, Constants.Id, false);
                    Store.Database.CommitTransaction();
                }
                catch (SmartStoreException sse)
                {
                    CreateLogItem("SmartStoreException", sse);
                    endWithError = true;
                }
                catch (Exception e)
                {
                    CreateLogItem("General exception", e);
                    endWithError = true;
                }
            }
            return endWithError;
        }
    }
}
