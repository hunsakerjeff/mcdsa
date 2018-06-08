using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Data.Services.API
{
    public class SearchContentDataService : ISearchContentDataService
    {
        private readonly ISearchTermDataService _searchTermDataService;

        private const string TagSoupName = "ContentDocumentTag";
        private const string TitleSoupName = "ContentDocumentTitle";
        private const string ProductTypeSoupName = "ContentDocumentProductType";
        private const string AssetTypeSoupName = "ContentDocumentAssetType";

        private const string ProductTypeKey = "ProductType";
        private const string TageKey = "Tag";
        private const string TitleKey = "Title";
        private const string AssetTypeKey = "AssetType";

        public SearchContentDataService(
            ISearchTermDataService searchTermDataService)
        {
            _searchTermDataService = searchTermDataService;
        }

        public async Task<IEnumerable<MediaLink>> FilterMediaLinkByQuery(IEnumerable<MediaLink> sourceLinks, string query)
        {
           await _searchTermDataService.SaveEventToSoup(query);
            _searchTermDataService.SyncUpSearchTerms();

            var searchTask = new List<Task<IList<string>>>
            {
               //SearchForDocumentIdByProductType(query),
               //SearchForDocumentIdByAssetType(query),
               SearchForDocumentIdByTag(query),
               SearchForDocumentIdByTitle(query)
            };
            var searchResults = await Task.WhenAll(searchTask);

            IEnumerable<string> result = new List<string>();
            result = searchResults.Aggregate(result, (current, searchResult) => current.Union(searchResult));
            return sourceLinks.Where(media => result.Contains(media.ID)).OrderBy(media => media.Name);
        }

        public async Task<IList<string>> SearchForDocumentIdByProductType(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                return SearchForDocumentIdByProductTypeSync(query);
            });
        }

        private static List<string> SearchForDocumentIdByProductTypeSync(string query)
        {
            var store = SmartStore.GetGlobalSmartStore();
            if (!store.HasSoup(ProductTypeSoupName))
                return new List<string>();

            var querySpec = QuerySpec.BuildContainQuerySpec(ProductTypeSoupName, ProductTypeKey, query, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(store);
            var results =
                store.Query(querySpec, 0)
                    .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentProductType>(item.ToString()))
                    .SelectMany(x => x.DocumentIds).Distinct().ToList();
            return results;
        }

        public async Task<IList<string>> SearchForDocumentIdByTag(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                return SearchForDocumentIdByTagSync(query);
            });
        }

        private static List<string> SearchForDocumentIdByTagSync(string query)
        {
            var store = SmartStore.GetGlobalSmartStore();
            if (!store.HasSoup(TagSoupName))
            {
                return new List<string>();
            }

            var querySpec = QuerySpec.BuildContainQuerySpec(TagSoupName, TageKey, query, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(store);

            var results = store.Query(querySpec, 0)
                                .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentsTag>(item.ToString()))
                                .SelectMany(x => x.DocumentIds).Distinct().ToList();

            return results;
        }

        private async Task<IList<string>> SearchForDocumentIdByAssetType(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                return SearchForDocumentIdByAssetTypeSync(query);
            });
        }

        private static List<string> SearchForDocumentIdByAssetTypeSync(string query)
        {
            var store = SmartStore.GetGlobalSmartStore();
            if (!store.HasSoup(AssetTypeSoupName))
            {
                return new List<string>();
            }

            var querySpec = QuerySpec.BuildContainQuerySpec(AssetTypeSoupName, AssetTypeKey, query, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(store);
            var results = store.Query(querySpec, 0)
                                .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentAssetType>(item.ToString()))
                                .SelectMany(x => x.DocumentIds).Distinct().ToList();
            return results;
        }

        private async Task<IList<string>> SearchForDocumentIdByTitle(string query)
        {
            return await Task.Factory.StartNew(() =>
            {
                return SearchForDocumentIdByTitleSync(query);
            });
        }

        private static List<string> SearchForDocumentIdByTitleSync(string query)
        {
            var store = SmartStore.GetGlobalSmartStore();
            if (!store.HasSoup(TitleSoupName))
            {
                return new List<string>();
            }

            string localQuery = query + "%";
            var querySpec = QuerySpec.BuildLikeQuerySpec(TitleSoupName, TitleKey, localQuery, QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(store);
            var results = store.Query(querySpec, 0)
                                .Select(item => CustomPrefixJsonConvert.DeserializeObject<DocumentTitle>(item.ToString()))
                                .SelectMany(x => x.DocumentIds).Distinct().ToList();

            return results;
        }
    }
}
