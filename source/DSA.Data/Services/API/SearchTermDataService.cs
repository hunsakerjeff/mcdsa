using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Data.Services.API
{
    public class SearchTermDataService : ISearchTermDataService
    {
        private const string SoupName = "SearchTerm";

        private static IndexSpec[] IndexSpecs => new[]
        {
            new IndexSpec(SyncManager.LocallyCreated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyUpdated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyDeleted, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.Local, SmartStoreType.SmartString),
            new IndexSpec(Constants.Id, SmartStoreType.SmartString)
        };

        public async Task SaveEventToSoup(string searchTerm)
        {
            await Task.Factory.StartNew(() =>
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if(!SfdcConfig.CollectSearchTerms)
                    return;

                var store = SmartStore.GetGlobalSmartStore();
                SetupSoupIfNotExistsNeeded(store, SoupName);
                
                var date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var searchTermRecord = new SearchTerms
                {
                    SearchTerm = searchTerm,
                    SearchTermDate = date,
                    Count = 1
                };

                var record = JObject.FromObject(searchTermRecord, JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));

                var info = searchTermRecord.GetType().GetTypeInfo().GetCustomAttributes()
               .SingleOrDefault(t => t is JsonObjectAttribute) as JsonObjectAttribute;

                if (info != null)
                {
                    record[Constants.SobjectType] = string.Format(info.Title, SfdcConfig.CustomerPrefix);
                }
                
                record[SyncManager.Local] = true;
                record[SyncManager.LocallyCreated] = true;
                record[SyncManager.LocallyUpdated] = false;
                record[SyncManager.LocallyUpdated] = false;
                record[Constants.Id] = Guid.NewGuid();
                store.Upsert(SoupName, record, Constants.Id, false);

                //SyncUpEvents();
            });
        }

        private static void SetupSoupIfNotExistsNeeded(ISmartStore store, string soupName)
        {
            if (!store.HasSoup(soupName))
            {
                store.RegisterSoup(soupName, IndexSpecs);
            }
        }

        public async void SyncUpSearchTerms()
        {
            try
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (!ObjectSyncDispatcher.HasInternetConnection() || !SfdcConfig.CollectSearchTerms)
                    return;

                await ObjectSyncDispatcher.Instance.SyncUpSearchTerms((text) => Debug.WriteLine(text));
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                Debug.WriteLine("SyncUpSearchTerms exception");
            }
        }
    }
}
