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
    public class CategoryMobileConfigDataService : ICategoryMobileConfigDataService
    {
        public async Task<List<CategoryMobileConfig>> GetCategoryMobileConfigs(string mobileAppConfigurationId)
        {
            if (string.IsNullOrWhiteSpace(mobileAppConfigurationId))
            {
                return new List<CategoryMobileConfig>();
            }

            return await Task.Factory.StartNew( () =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildExactQuerySpec(CategoryMobileConfig.SoupName, CategoryMobileConfig.MobileAppConfigurationIdIndexKey, mobileAppConfigurationId, SfdcConfig.PageSize).RemoveLimit(globalStore);
                
                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<CategoryMobileConfig>(item.ToString()))
                        .ToList();
            });
        }
    }
}
