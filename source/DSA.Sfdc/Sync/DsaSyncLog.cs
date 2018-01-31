using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Sfdc.Sync
{
    public class DsaSyncLog : SObject
    {
        private static readonly Lazy<DsaSyncLog> Lazy = new Lazy<DsaSyncLog>(() => new DsaSyncLog(SmartStore.GetGlobalSmartStore()));

        public static DsaSyncLog Instance => Lazy.Value;

        private bool _isSyncStarted;
        private SyncLog _logObject;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec(Constants.Id, SmartStoreType.SmartString)
        };

        internal override bool DeleteAfterSyncUp { get; set; } = true;

        internal override List<string> FieldsToSyncUp { get; set; } =
            JObject.FromObject(new SyncLog(), JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings))
                    .Properties().Select(p => p.Name).ToList();

        private DsaSyncLog(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null)
                AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public bool IsSyncStarted()
        {
            return _isSyncStarted;
        }

        public void StartSynchronization(SyncLog log)
        {
            if (!SfdcConfig.SyncLogsEnable)
                return;

            _logObject = log;
            _isSyncStarted = true;
        }

        public void SetDownloadedFilesInfo(int filesToDownload, long contentSize)
        {
            if (!_isSyncStarted || !SfdcConfig.SyncLogsEnable)
                return;

            _logObject.SizeOfContent = contentSize;
            _logObject.FilesToDownload = filesToDownload;
        }

        public void SynchronizationStopped(SyncStatus status, Exception exception = null)
        {
            if (!_isSyncStarted || !SfdcConfig.SyncLogsEnable)
                return;

            _logObject.SyncStatus = status;
            if (_logObject.SyncStatus == SyncStatus.Failed)
            {
                _logObject.FailureErrorMessag = HandleExceptionMessage(exception);
            }
            StopSynchronization();
            SaveLog(_logObject);
        }

        private void StopSynchronization()
        {
            _logObject.EndTime = DateTime.UtcNow;
            _logObject.Duration = (_logObject.EndTime - _logObject.StartTime).TotalSeconds;
            _isSyncStarted = false;
        }

        private void SaveLog(SyncLog log)
        {
            RegisterSoup();

            var record = JObject.FromObject(log, JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));

            record[SyncManager.Local] = true;

            var info = log.GetType().GetTypeInfo().GetCustomAttributes()
                .SingleOrDefault(t => t is JsonObjectAttribute) as JsonObjectAttribute;

            if (info != null)
            {
                record[Constants.SobjectType] = string.Format(info.Title, SfdcConfig.CustomerPrefix);
            }

            record[SyncManager.LocallyCreated] = true;
            record[SyncManager.LocallyUpdated] = false;
            record[SyncManager.LocallyUpdated] = false;
            record[Constants.Id] = Guid.NewGuid();
            Store.Upsert(SoupName, record, Constants.Id, false);
        }

        private string HandleExceptionMessage(Exception exception)
        {
            if (exception == null)
                return "Synchronization canceled by user";

            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                var messages = aggregateException.InnerExceptions.Select(x => x.Message);
                return string.Join("\n", messages);
            }
            else
            {
                return exception.Message;
            }
        }
    }
}
