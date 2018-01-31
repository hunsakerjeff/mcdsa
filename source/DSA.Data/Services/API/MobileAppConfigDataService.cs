using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Data.Services.API
{
    public class MobileAppConfigDataService : IMobileAppConfigDataService
    {
        public async Task<List<MobileAppConfig>> GetMobileAppConfigs()
        {
            return await Task.Factory.StartNew(() => 
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec(MobileAppConfig.SoupName, "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                            .Query(querySpec, 0)
                            .Select(item => CustomPrefixJsonConvert.DeserializeObject<MobileAppConfig>(item.ToString()))
                            .ToList();
            });
        }

        public async Task<MobileAppConfig> GetMobileAppConfig(string configID)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildExactQuerySpec(MobileAppConfig.SoupName, "Id", configID, SfdcConfig.PageSize);

                return globalStore
                            .Query(querySpec, 0)
                            .Select(item => CustomPrefixJsonConvert.DeserializeObject<MobileAppConfig>(item.ToString()))
                            .FirstOrDefault();
            });
        }
    }
}
