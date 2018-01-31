using System;
using System.Linq;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    public class LastLoggedAccountState
    {
        private static readonly Lazy<LastLoggedAccountState> Lazy = new Lazy<LastLoggedAccountState>(() => new LastLoggedAccountState());

        public static LastLoggedAccountState Instance => Lazy.Value;

        private readonly SmartStore _store;

        private LastLoggedAccountState()
        {
            _store = SmartStore.GetGlobalSmartStore();
            SetupSyncsSoupIfNotExistsNeeded();
        }

        public string SoupName => GetType().Name;

        public JObject SaveStateToSoup(Account account)
        {
            if (string.IsNullOrWhiteSpace(account?.UserName) || string.IsNullOrWhiteSpace(account.LoginUrl))
            {
                throw new SyncException("Only Account with username and loginUrl can be saved to soup.");
            }

            SetupSyncsSoupIfNotExistsNeeded();
            var loginItem = new LoginAccount()
            {
                UserName = account.UserName,
                LoginUrl = account.LoginUrl
            };

            var sync = JObject.FromObject(loginItem);

            return _store.Upsert(SoupName, sync, "UserName");
        }

        public bool CheckIfAccountExistsInSoup(Account account)
        {
            SetupSyncsSoupIfNotExistsNeeded();
            var querySpec = QuerySpec.BuildAllQuerySpec("LastLoggedAccountState", "UserName", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(_store);
            
            var result = _store
                    .Query(querySpec, 0)
                    .Select(item => JsonConvert.DeserializeObject<LoginAccount>(item.ToString()))
                    .ToList();
            return result.SingleOrDefault(item
                => item.UserName.Equals(account.UserName, StringComparison.OrdinalIgnoreCase)
                && item.LoginUrl.Equals(account.LoginUrl, StringComparison.OrdinalIgnoreCase)) != null;
        }

        private void SetupSyncsSoupIfNotExistsNeeded()
        {
            IndexSpec[] indexSpecs =
            {
                new IndexSpec("UserName", SmartStoreType.SmartString)
            };

            if (!_store.HasSoup(SoupName))
            {
                _store.RegisterSoup(SoupName, indexSpecs);
            }
        }
    }
}
