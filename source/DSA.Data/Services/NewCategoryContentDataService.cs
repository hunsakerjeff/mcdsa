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

namespace DSA.Data.Services
{
    public class NewCategoryContentDataService : INewCategoryContentDataService
    {
        public async Task<List<CategoryContent>> GetCategoryContent(string mobileConfigurationID)
        {
            return await Task.Factory.StartNew(() =>
            {
                var smartSql = @"SELECT 
                                    {" + NewCategoryContent.SoupName + @":_soup}	
                                FROM 
                                    {" + Category.SoupName + @"}
	                                JOIN {" + CategoryMobileConfig.SoupName + @"} 
                                        ON {" + CategoryMobileConfig.SoupName + @"}.{" + CategoryMobileConfig.SoupName + @":" + CategoryMobileConfig.CategoryIdIndexKey + @"} = {" + Category.SoupName + @"}.{" + Category.SoupName + @":Id}
                                    JOIN  {" + NewCategoryContent.SoupName + @"}
                                        ON   {" + NewCategoryContent.SoupName + @":" + CategoryContent.CategoryIdIndexKey + "} = {" + Category.SoupName + @"}.{" + Category.SoupName + @":Id}
                                 WHERE
                                    {" + CategoryMobileConfig.SoupName + @":" + CategoryMobileConfig.MobileAppConfigurationIdIndexKey + @"} = '" + mobileConfigurationID + "'";
                
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec2 = QuerySpec.BuildSmartQuerySpec(smartSql, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                         .Query(querySpec2, 0)
                         .Select(item => CustomPrefixJsonConvert.DeserializeObject<CategoryContent>(item[0].ToObject<JObject>().ToString()))
                         .ToList();
            });
        }
    }
}

