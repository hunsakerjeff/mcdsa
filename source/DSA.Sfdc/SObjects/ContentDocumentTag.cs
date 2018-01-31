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
    internal class ContentDocumentTag : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Tag", SmartStoreType.SmartString)
        };

        private static readonly Lazy<ContentDocumentTag> Lazy = new Lazy<ContentDocumentTag>(() => new ContentDocumentTag(SmartStore.GetGlobalSmartStore()));

        public static ContentDocumentTag Instance => Lazy.Value;

        public ContentDocumentTag(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null)
                AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public async Task<IList<DocumentsTag>> GetAll()
        {
            return await Task.Factory.StartNew(() =>
            {
                if (!Store.HasSoup(SoupName))
                    return new List<DocumentsTag>();

                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Tag", QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentsTag>(item.ToString()))
                        .ToList();
                return results;
            });
        }

        public async Task<IList<string>> SearchForDocumentIdByTag(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                var querySpec = QuerySpec.BuildLikeQuerySpec(SoupName, "Tag", query, QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentsTag>(item.ToString()))
                        .SelectMany(x => x.DocumentIds).Distinct().ToList();
                return results;
            });
        }

        public async Task SaveTagsToSoup(string tags, string docId, CancellationToken token = default(CancellationToken))
        {
            await Task.Factory.StartNew(() =>
            {
                if (string.IsNullOrWhiteSpace(tags))
                    return;

                SetupSoupIfNotExistsNeeded();
                var tagsArray = tags.Split(',');
                foreach (var tag in tagsArray)
                {
                    var querySpec = QuerySpec.BuildExactQuerySpec(SoupName, "Tag", tag, 1);
                    var results = Store?.Query(querySpec, 0).Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentsTag>(item.ToString())).FirstOrDefault();
                    var docTag = new DocumentsTag
                    {
                        Tag = tag, DocumentIds = new List<string> { docId }
                    };

                    if (results != null)
                    {
                        if (results.DocumentIds.Contains(docId))
                        {
                            continue;
                        }
                        results.DocumentIds.Add(docId);
                        docTag.DocumentIds = results.DocumentIds;
                    }

                    var jTag = JObject.FromObject(docTag);
                    Store?.Upsert(SoupName, jTag, "Tag");
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
