using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Model;

namespace DSA.Sfdc.Sync
{
    public class SuccessfulSyncState
    {
        public enum SyncedObjectMetaType
        {
            MobileAppConfigs,
            CategoryMobileConfigs,
            ContentDocuments,
            ContentThumbnails,
            ContentDistribution
        }

        private static readonly Lazy<SuccessfulSyncState> Lazy = new Lazy<SuccessfulSyncState>(() => new SuccessfulSyncState());

        public static SuccessfulSyncState Instance => Lazy.Value;

        private readonly SmartStore _store;

        private SuccessfulSyncState()
        {
            _store = SmartStore.GetGlobalSmartStore();
            SetupSyncsSoupIfNotExists();
        }

        public string SoupName => GetType().Name;

        public JObject SaveStateToSoupForMetaTransaction(SyncState state, SyncedObjectMetaType syncObjectMetaType)
        {
            return SaveStateToSoup(state, syncObjectMetaType.ToString());
        }

        public JObject SaveStateToSoupForAttTransaction(SyncState state, string transactionItemType)
        {
            if (string.IsNullOrWhiteSpace(transactionItemType))
            {
                throw new SyncException("Expected transactionItemType.");
            }

            return SaveStateToSoup(state, transactionItemType);
        }

        public JObject SaveStateToSoupForAttTransaction(SyncState state, string transactionItemType, DateTime lastModifiedDate)
        {
            if (string.IsNullOrWhiteSpace(transactionItemType))
            {
                throw new SyncException("Expected transactionItemType.");
            }

            return SaveStateToSoup(state, transactionItemType, lastModifiedDate);
        }

        public JObject SaveStateToSoupForDocTransaction(SyncState state, ContentDocument cd)
        {
            if (cd == null)
            {
                throw new SyncException("ContentDocument is required to save transaction.");
            }

            return SaveStateToSoup(state, cd.Id, cd.LatestPublishedVersionId);
        }

        private JObject SaveStateToSoup(SyncState state, string transactionItemType)
        {
            return SaveStateToSoup(state, transactionItemType, string.Empty);
        }

        private JObject SaveStateToSoup(SyncState state, string transactionItemType, string docVersionId)
        {
            if (state == null || state.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Only SyncStatusTypes.Done can be saved to soup.");
            }
            
            var syncItem = new SuccessfulSync()
            {
                SyncId = state.Id.ToString(),
                TransactionItemType = transactionItemType,
                DocVersionId = docVersionId
            };

            var sync = JObject.FromObject(syncItem);

            return _store.Upsert(SoupName, sync, "TransactionItemType");
        }

        private JObject SaveStateToSoup(SyncState state, string transactionItemType, DateTime lastModifiedDate)
        {
            if (state == null || state.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Only SyncStatusTypes.Done can be saved to soup.");
            }

            var syncItem = new SuccessfulSync()
            {
                SyncId = state.Id.ToString(),
                TransactionItemType = transactionItemType,
                LastModifiedDate = lastModifiedDate
            };

            var sync = JObject.FromObject(syncItem);

            return _store.Upsert(SoupName, sync, "TransactionItemType");
        }

        public void RecreateClearSoup()
        {
            if (_store.HasSoup(SoupName))
            {
                _store.DropSoup(SoupName);
            }

            SetupSyncsSoupIfNotExists();
        }

        public void RevertSoup(IList<SuccessfulSync> successfulSyncs)
        {
            if (successfulSyncs != null)
            {
                RecreateClearSoup();
                foreach (var successfulSync in successfulSyncs)
                {
                    _store.Upsert(SoupName, JObject.FromObject(successfulSync), "TransactionItemType");
                }
            }
        }

        private void SetupSyncsSoupIfNotExists()
        {
            IndexSpec[] indexSpecs =
            {
                new IndexSpec(SuccessfulSync.TransactionItemTypeIndexKey, SmartStoreType.SmartString),
                new IndexSpec(SuccessfulSync.SyncIdIndexKey, SmartStoreType.SmartString),
            };

            if (!_store.HasSoup(SoupName))
            {
                _store.RegisterSoup(SoupName, indexSpecs);
            }
        }

        public IList<SuccessfulSync> GetAllSuccessfulSyncsFromSoup()
        {
            SetupSyncsSoupIfNotExists();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, SuccessfulSync.SyncIdIndexKey, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(_store);
            var results = _store.Query(querySpec, 0);

            var macList = results.Select(item => CustomPrefixJsonConvert.DeserializeObject<SuccessfulSync>(item.ToString())).ToList();

            return macList;
        }

        public Dictionary<string, SuccessfulSync> GetAllVersionedsSyncsFromSoupIndexedByDocId()
        {
            SetupSyncsSoupIfNotExists();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, SuccessfulSync.SyncIdIndexKey, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(_store);
            var results = _store.Query(querySpec, 0);

            var allTransactions = results.Select(item => CustomPrefixJsonConvert.DeserializeObject<SuccessfulSync>(item.ToString()));
            var onlyVersionedTrans = allTransactions.Where(item => !string.IsNullOrEmpty(item.TransactionItemType)).ToList();

            return onlyVersionedTrans.ToDictionary(docTrans => docTrans.TransactionItemType);
        }
    }
}
