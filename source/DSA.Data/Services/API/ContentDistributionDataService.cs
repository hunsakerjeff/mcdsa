using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Data.Services.API
{
    public class ContentDistributionDataService : IContentDistributionDataService
    {
        private const string SoupName = "ContentDistribution";
        private const string Key = "ContentVersionId";

        public async Task<List<ContentDistribution>> GetAllContentDistributions()
        {
            return await Task.Factory.StartNew(() =>
            {
                if (!SfdcConfig.EmailOnlyContentDistributionLinks)
                {
                    return new List<ContentDistribution>();
                }

                var store = SmartStore.GetGlobalSmartStore();

                if(!store.HasSoup(SoupName))
                    return new List<ContentDistribution>();

                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, Key, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(store);
                return store.Query(querySpec, 0)
                            .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContentDistribution>(item.ToString()))
                            .Where(x => !x.IsDeleted && (x.ExpiryDate == null || x.ExpiryDate > DateTime.Now))
                            .ToList();
            });
        }
    }
}
