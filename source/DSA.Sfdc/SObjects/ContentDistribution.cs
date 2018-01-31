using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Sfdc.SObjects
{
    /// <summary>
    /// ContentDistribution
    /// Represents information about sharing a document externally
    /// https://developer.salesforce.com/docs/atlas.en-us.api.meta/api/sforce_api_objects_contentdistribution.htm
    /// </summary>
    internal class ContentDistribution : SObject
    {
        private const string Key = "ContentVersionId";

        private const string SoupId = "_soupEntryId";

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
       {
            new IndexSpec(Key, SmartStoreType.SmartString),
            new IndexSpec(Constants.Id, SmartStoreType.SmartString),
       };

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT Id, ContentVersionId, " +
                            "DistributionPublicUrl, " +
                            "ExpiryDate, IsDeleted, " +
                            "Name " +
                            "FROM ContentDistribution " +
                            "WHERE " +
                            "ContentVersion.{0}__Available_Offline__c = true";

                return string.Format(query, Prefix);
            }
        }

        public ContentDistribution(SmartStore store, bool clearSoup = true) : base(store)
        {
            if (IndexedFieldsForSObjects != null)
                AddIndexSpecItems(IndexedFieldsForSObjects);

            ClearSoup = clearSoup;
        }

        public async Task ClearNotNeededContentDistribution(IList<Model.Models.ContentDocument> contentDocuments)
        {
            await Task.Factory.StartNew(() =>
            {
                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, Key, QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
                var results = Store.Query(querySpec, 0);
                var distributionSoupObjectsList = results.Select(x => x.ToObject<JObject>()).ToList();

                var versionIdsList = contentDocuments.Select(x => x.LatestPublishedVersionId);

                var distributionListToDelete = distributionSoupObjectsList
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DSA.Model.Dto.ContentDistribution>(item.ToString()))
                        .Where(x => x.IsDeleted || x.ExpiryDate <= DateTime.Now || !versionIdsList.Contains(x.ContentVersionId))
                        .Select(x => x.Id)
                        .ToList();

                var soupIds = distributionSoupObjectsList.Select(x => new {SoupId = x.ExtractValue<long>(SoupId), Id = x.ExtractValue<string>(Constants.Id) })
                        .Where(x => distributionListToDelete.Contains(x.Id))
                        .Select(x => x.SoupId).ToArray();

                Store.Delete(SoupName, soupIds, false);
            });
        }
    }
}
