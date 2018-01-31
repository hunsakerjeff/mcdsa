using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Sfdc.SObjects;
using Salesforce.SDK.SmartStore.Store;
using ContentDocument = DSA.Model.Models.ContentDocument;

namespace DSA.Sfdc.Sync
{
    public class DocIndexDeltaSieve : IResultSieveAsync<IList<ContentDocument>>
    {
        private readonly SmartStore _store;

        public DocIndexDeltaSieve(SmartStore store)
        {
            if (store == null)
            {
                throw new SyncException("store param is required to filtered data.");
            }
            _store = store;
        }


        public async Task<IList<ContentDocument>> GetFilteredResult(IList<ContentDocument> resultUnfiltered)
        {
            //var documentAssetType = new ContentDocumentAssetType(_store);
            //var documentProductType = new ContentDocumentProductType(_store);

            var checkTask = new List<Task<IList<string>>>
            {
               FilterByTitle(resultUnfiltered),
               FilterByTags(resultUnfiltered),
               FilterByProductType(resultUnfiltered),
               FilterByAssetType(resultUnfiltered)
            };

            var checkResults = await Task.WhenAll(checkTask);
            IEnumerable<string> ids = new List<string>();
            ids = checkResults.Aggregate(ids, (current, searchResult) => current.Union(searchResult));
            var result = resultUnfiltered.Where(x => ids.Contains(x.Id)).ToList();
            return result;
        }

        private async Task<IList<string>> FilterByProductType(IList<ContentDocument> resultUnfiltered)
        {
            var documentProductType = new ContentDocumentProductType(_store);
            var documentProductInSoupTask = documentProductType.GetAll();
            var docTypes = resultUnfiltered.Select(doc => new
            {
                doc.Id,
                Type = string.IsNullOrWhiteSpace(doc.AssetType) ? new List<string>() : doc.AssetType.Split(';').ToList()
            }).Where(doc => doc.Type.Any());

            var filteredIds = new List<string>();

            var documentProductInSoup = await documentProductInSoupTask;

            foreach (var doc in docTypes)
            {
                foreach (var type in doc.Type)
                {
                    if (!documentProductInSoup.Select(x => x.ProductType).Contains(type))
                    {
                        filteredIds.Add(doc.Id);
                    }
                    else if (!documentProductInSoup.FirstOrDefault(x => x.ProductType == type).DocumentIds.Contains(doc.Id))
                    {
                        filteredIds.Add(doc.Id);
                    }
                }
            }
            return filteredIds.Distinct().ToList();
        }

        private async Task<IList<string>> FilterByAssetType(IList<ContentDocument> resultUnfiltered)
        {
            var documentAssetType = new ContentDocumentAssetType(_store);
            var assetTypeInSoupTask = documentAssetType.GetAll();
            var docTypes = resultUnfiltered.Select(doc => new
            {
                Id = doc.Id,
                Type = string.IsNullOrWhiteSpace(doc.AssetType) ? new List<string>() : doc.AssetType.Split(';').ToList()
            }).Where(doc => doc.Type.Any());

            var filteredIds = new List<string>();

            var assetTypeInSoup = await assetTypeInSoupTask;

            foreach (var doc in docTypes)
            {
                foreach (var type in doc.Type)
                {
                    if (!assetTypeInSoup.Select(x => x.AssetType).Contains(type))
                    {
                        filteredIds.Add(doc.Id);
                    }
                    else if (!assetTypeInSoup.FirstOrDefault(x => x.AssetType == type).DocumentIds.Contains(doc.Id))
                    {
                        filteredIds.Add(doc.Id);
                    }
                }
            }
            return filteredIds.Distinct().ToList();
        }

        private async Task<IList<string>> FilterByTitle(IList<ContentDocument> resultUnfiltered)
        {
            var documentTitle = new ContentDocumentTitle(_store);
            var titlesInSoupTask = documentTitle.GetAll();
            var docTitles = resultUnfiltered.Select(doc => new
            {
                Id = doc.Id,
                Title = doc.Title
            }).Where(doc => !string.IsNullOrWhiteSpace(doc.Title));

            var filteredIds = new List<string>();

            var titlesInSoup = await titlesInSoupTask;

            foreach (var doc in docTitles)
            {
                if (titlesInSoup.FirstOrDefault(x => x.Title == doc.Title) == null)
                {
                    filteredIds.Add(doc.Id);
                }
                else if (!titlesInSoup.FirstOrDefault(x => x.Title == doc.Title).DocumentIds.Contains(doc.Id))
                {
                    filteredIds.Add(doc.Id);
                }
            }
            return filteredIds.Distinct().ToList();
        }

        private async Task<IList<string>> FilterByTags(IList<ContentDocument> resultUnfiltered)
        {
            var documentTag = new ContentDocumentTag(_store);
            var tagsInSoupTask = documentTag.GetAll();
            var docTags = resultUnfiltered.Select(doc => new
            {
                Id = doc.Id,
                Tags = string.IsNullOrWhiteSpace(doc.Tags) ? new List<string>() : doc.Tags.Split(',').ToList()
            }).Where(doc => doc.Tags.Any());

            var filteredIds = new List<string>();

            var tagsInSoup = await tagsInSoupTask;

            foreach (var doc in docTags)
            {
                foreach (var tag in doc.Tags)
                {
                    if (!tagsInSoup.Select(x => x.Tag).Contains(tag))
                    {
                        filteredIds.Add(doc.Id);
                    }
                    else if (!tagsInSoup.FirstOrDefault(x => x.Tag == tag).DocumentIds.Contains(doc.Id))
                    {
                        filteredIds.Add(doc.Id);
                    }
                }
            }
            return filteredIds.Distinct().ToList();
        }
    }
}
