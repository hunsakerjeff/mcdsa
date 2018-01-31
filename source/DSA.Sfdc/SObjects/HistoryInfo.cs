using System;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using DTO = DSA.Model.Dto;

namespace DSA.Sfdc.SObjects
{
    public class HistoryInfo
    {
        private static readonly Lazy<HistoryInfo> Lazy = new Lazy<HistoryInfo>(() => new HistoryInfo());

        public static HistoryInfo Instance => Lazy.Value;

        private readonly IndexSpec[] _indexSpecs = { new IndexSpec("Id", SmartStoreType.SmartString) };

        private readonly SmartStore _store;

        private HistoryInfo()
        {
            _store = SmartStore.GetGlobalSmartStore();
            SetupSyncsSoupIfNotExistsNeeded();
        }

        public string SoupName => GetType().Name;

        public JObject SaveToSoup(DTO.HistoryInfoDto settings)
        {
            var sync = JObject.FromObject(settings);
            SetupSyncsSoupIfNotExistsNeeded();
            return _store.Upsert(SoupName, sync, "Id");
        }

        private void SetupSyncsSoupIfNotExistsNeeded()
        {
            if (_store.HasSoup(SoupName))
            {
                return;
            }

            _store.RegisterSoup(SoupName, _indexSpecs);
        }
    }
}
