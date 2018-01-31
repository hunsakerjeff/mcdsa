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
    internal class ContentDocumentAssetType : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("AssetType", SmartStoreType.SmartString)
        };

        private static readonly Lazy<ContentDocumentAssetType> Lazy =
            new Lazy<ContentDocumentAssetType>(() => new ContentDocumentAssetType(SmartStore.GetGlobalSmartStore()));

        public static ContentDocumentAssetType Instance => Lazy.Value;

        public ContentDocumentAssetType(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public async Task<IList<string>> SearchForDocumentIdByAssetType(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                var querySpec = QuerySpec.BuildLikeQuerySpec(SoupName, "AssetType", query, QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentAssetType>(item.ToString()))
                        .SelectMany(x => x.DocumentIds).Distinct().ToList();
                return results;
            });
        }

        public async Task<IList<DocumentAssetType>> GetAll()
        {
            return await Task.Factory.StartNew(() =>
            {
                if (Store.HasSoup(SoupName))
                    return new List<DocumentAssetType>();

                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "AssetType", QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentAssetType>(item.ToString()))
                        .ToList();
                return results;
            });
        }


        public async Task SaveAssetTypeToSoup(string assetTypes, string docId, CancellationToken token = default(CancellationToken))
        {
            await Task.Factory.StartNew(() =>
            {
                if (string.IsNullOrWhiteSpace(assetTypes))
                    return;

                SetupSoupIfNotExistsNeeded();

                var assetArray = assetTypes.Split(';');
                foreach (var assetType in assetArray)
                {
                    var querySpec = QuerySpec.BuildExactQuerySpec(SoupName, "AssetType", assetType, 1);
                    var results = Store?.Query(querySpec, 0)
                                .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentAssetType>(item.ToString()))
                                .FirstOrDefault();
                    var docTitle = new DocumentAssetType
                    {
                        AssetType = assetType,
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
                    Store?.Upsert(SoupName, jTag, "AssetType");
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
