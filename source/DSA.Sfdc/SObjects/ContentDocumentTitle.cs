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
    internal class ContentDocumentTitle : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Title", SmartStoreType.SmartString)
        };

        private static readonly Lazy<ContentDocumentTitle> Lazy =
            new Lazy<ContentDocumentTitle>(() => new ContentDocumentTitle(SmartStore.GetGlobalSmartStore()));

        public static ContentDocumentTitle Instance => Lazy.Value;


        public ContentDocumentTitle(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public async Task<IList<DocumentTitle>> GetAll()
        {
            return await Task.Factory.StartNew(() =>
            {
                if (!Store.HasSoup(SoupName))
                    return new List<DocumentTitle>();

                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Title", QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentTitle>(item.ToString()))
                        .ToList();
                return results;
            });
        }

        public async Task<IList<string>> SearchForDocumentIdByTitle(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                var querySpec = QuerySpec.BuildLikeQuerySpec(SoupName, "Title", query, QuerySpec.SqlOrder.ASC, PageSize);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentTitle>(item.ToString()))
                        .SelectMany(x => x.DocumentIds).Distinct().ToList();
                return results;
            });
        }

        public async Task SaveTitleToSoup(string title, string docId, CancellationToken token = default(CancellationToken))
        {
            await Task.Factory.StartNew(() =>
            {
                if (string.IsNullOrWhiteSpace(title))
                    return;

                SetupSoupIfNotExistsNeeded();

                var querySpec = QuerySpec.BuildExactQuerySpec(SoupName, "Title", title, 1);
                var results =
                    Store?.Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentTitle>(item.ToString()))
                        .FirstOrDefault();
                var docTitle = new DocumentTitle
                {
                    Title = title,
                    DocumentIds = new List<string> {docId}
                };

                if (results != null)
                {
                    if (results.DocumentIds.Contains(docId))
                    {
                        return;
                    }
                    results.DocumentIds.Add(docId);
                    docTitle.DocumentIds = results.DocumentIds;
                }

                var jTag = JObject.FromObject(docTitle);
                Store?.Upsert(SoupName, jTag, "Title");
                
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
