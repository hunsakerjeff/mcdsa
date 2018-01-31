using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Data.Services.API
{
    public class SyncDataService : ISyncDataService
    {
        public async Task<List<SuccessfulSync>> GetSyncConfigs(IEnumerable<string> typeOfObjList)
        {
            return await Task.Factory.StartNew(() =>
            {
                if (typeOfObjList.Any() == false)
                {
                    return new List<SuccessfulSync>();
                }

                var inCondition = string.Join(",", typeOfObjList.Select(s => $"'{s}'"));
                var smartQuery = "SELECT {" + SuccessfulSync.SoupName + ":_soup} FROM {" + SuccessfulSync.SoupName + "}  WHERE {" + SuccessfulSync.SoupName + ":" + SuccessfulSync.TransactionItemTypeIndexKey + "} IN (" + inCondition + ")";

                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildSmartQuerySpec(smartQuery, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<SuccessfulSync>(item[0].ToObject<JObject>().ToString()))
                        .ToList();
            });
        }

        public async Task<List<SuccessfulSync>> GetSyncConfigs(string syncID)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildExactQuerySpec(SuccessfulSync.SoupName, SuccessfulSync.SyncIdIndexKey, syncID, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<SuccessfulSync>(item.ToString()))
                        .ToList();
            });
        }

        public async Task<SuccessfulSync> GetSyncConfig(string typeOfObj)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildExactQuerySpec(SuccessfulSync.SoupName, SuccessfulSync.TransactionItemTypeIndexKey, typeOfObj, SfdcConfig.PageSize);
                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<SuccessfulSync>(item.ToString()))
                        .FirstOrDefault();
            });
        }
    }
}
