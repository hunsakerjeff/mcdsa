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
    public class CategoryDataService : ICategoryDataService
    {
        public async Task<List<Category>> GetCategories(string mobileConfigurationID)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                var smartSql = @"SELECT 
                                    {"+ Category.SoupName + @":_soup} 	
                                FROM 
                                    {" + Category.SoupName + @"}
	                                JOIN {" + CategoryMobileConfig.SoupName + @"} 
                                        ON {" + CategoryMobileConfig.SoupName + @"}.{" + CategoryMobileConfig.SoupName + @":"+ CategoryMobileConfig.CategoryIdIndexKey + @"} = {" + Category.SoupName + @"}.{" + Category.SoupName + @":Id}
                                 WHERE
                                    {" + CategoryMobileConfig.SoupName + @":" + CategoryMobileConfig.MobileAppConfigurationIdIndexKey + @"} = '" + mobileConfigurationID+"'";

                var querySpec = QuerySpec.BuildSmartQuerySpec(smartSql, SfdcConfig.PageSize).RemoveLimit(globalStore);
             
                return globalStore
                           .Query(querySpec, 0)
                           .Select(item => CustomPrefixJsonConvert.DeserializeObject<Category>(item[0].ToObject<JObject>().ToString()))
                           .ToList();
            });
        }
    }
}
