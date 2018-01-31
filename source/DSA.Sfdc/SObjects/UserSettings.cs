using System;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using DTO = DSA.Model.Dto;

namespace DSA.Sfdc.SObjects
{
    public class UserSettings
    {
        private static readonly Lazy<UserSettings> Lazy = new Lazy<UserSettings>(() => new UserSettings());

        public static UserSettings Instance => Lazy.Value;

        private readonly SmartStore _store;

        private UserSettings()
        {
            _store = SmartStore.GetGlobalSmartStore();
            SetupSyncsSoupIfNotExistsNeeded();
        }

        public string SoupName => GetType().Name;

        public JObject SaveToSoup(DTO.UserSettingsDto settings)
        {
            var sync =  JObject.FromObject(settings);
            SetupSyncsSoupIfNotExistsNeeded();
            return _store.Upsert(SoupName, sync, "UserId");
        }

        private void SetupSyncsSoupIfNotExistsNeeded()
        {
            IndexSpec[] indexSpecs =
            {
                new IndexSpec("UserId", SmartStoreType.SmartString)
            };

            if (!_store.HasSoup(SoupName))
            {
                _store.RegisterSoup(SoupName, indexSpecs);
            }
        }
    }
}
