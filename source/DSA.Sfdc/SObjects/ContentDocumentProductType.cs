using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class ContentDocumentProductType : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("ProductType", SmartStoreType.SmartString)
        };

        private static readonly Lazy<ContentDocumentProductType> Lazy =
            new Lazy<ContentDocumentProductType>(() => new ContentDocumentProductType(SmartStore.GetGlobalSmartStore()));

        public static ContentDocumentProductType Instance => Lazy.Value;

        public ContentDocumentProductType(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public async Task<IList<DocumentProductType>> GetAll()
        {
            return await Task.Factory.StartNew(() =>
            {
                if (!Store.HasSoup(SoupName))
                    return new List<DocumentProductType>();

                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "ProductType", QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentProductType>(item.ToString()))
                        .ToList();
                return results;
            });
        }

        public async Task<IList<string>> SearchForDocumentIdByProductType(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                var querySpec = QuerySpec.BuildLikeQuerySpec(SoupName, "ProductType", query, QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentProductType>(item.ToString()))
                        .SelectMany(x => x.DocumentIds).Distinct().ToList();
                return results;
            });
        }

        public async Task SaveProductTypeToSoup(string productTypes, string docId, CancellationToken token = default(CancellationToken))
        {
            await Task.Factory.StartNew(() =>
            {
                if (string.IsNullOrWhiteSpace(productTypes))
                    return;

                SetupSoupIfNotExistsNeeded();

                var productTypeArray = productTypes.Split(';');
                foreach (var productType in productTypeArray)
                {
                    var querySpec = QuerySpec.BuildExactQuerySpec(SoupName, "ProductType", productType, 1);
                    var results =
                        Store?.Query(querySpec, 0)
                            .Select(
                                item => CustomPrefixJsonConvert.DeserializeObject<DocumentProductType>(item.ToString()))
                            .FirstOrDefault();
                    var docTitle = new DocumentProductType
                    {
                        ProductType = productType,
                        DocumentIds = new List<string> {docId}
                    };

                    if (results != null)
                    {
                        if (results.DocumentIds.Contains(docId))
                        {
                            continue;
                        }
                        results.DocumentIds.Add(docId);
                        docTitle.DocumentIds = results.DocumentIds;
                    }

                    var jTag = JObject.FromObject(docTitle);
                    Store?.Upsert(SoupName, jTag, "ProductType");
                }
            }, token);
        }

        private void SetupSoupIfNotExistsNeeded()
        {
            if (!Store.HasSoup(SoupName))
            {
                Store.RegisterSoup(SoupName, IndexSpecs);
            }
        }
    }
}
