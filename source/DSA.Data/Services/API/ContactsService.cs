using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Data.Services.API
{
    public class ContactsService : IContactsService
    {
        private const string ContactSoupName = "Contact";
        private const string RecentContactSoupName = "RecentContact";

        public async Task<List<ContactDTO>> GetAllContacts()
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                var querySpec = QuerySpec.BuildAllQuerySpec(ContactSoupName, Constants.Id, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore.Query(querySpec, 0)
                            .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContactDTO>(item.ToString()))
                            .ToList();
            });
        }

        public async Task<List<ContactDTO>> GetRecentContacts()
        {
            return await Task.Factory.StartNew(() =>
            {
                var store = SmartStore.GetGlobalSmartStore();
                CheckIfNeededSoupExists(store, RecentContactSoupName);
                var querySpec = QuerySpec.BuildAllQuerySpec(RecentContactSoupName, Constants.Id, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(store);
                return store.Query(querySpec, 0)
                            .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContactDTO>(item.ToString()))
                            .ToList();
            });
        }

        public async Task AddRecentContact(ContactDTO contact)
        {
            await Task.Factory.StartNew(() =>
            {

                var store = SmartStore.GetGlobalSmartStore();
                CheckIfNeededSoupExists(store, RecentContactSoupName);
                var record = JObject.FromObject(contact,
                    JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));
                store.Upsert(RecentContactSoupName, record, Constants.Id, false);
            });
        }

        private static void CheckIfNeededSoupExists(ISmartStore store, string soupName)
        {
            if (!store.HasSoup(soupName))
            {
                store.RegisterSoup(soupName, new[] { new IndexSpec("Id", SmartStoreType.SmartString)});
            }
        }
    }
}
