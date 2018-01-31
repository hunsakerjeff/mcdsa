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
    public class ContentDocumentDataService : IContentDocumentDataService
    {
        private readonly string _soupName = ContentDocument.SoupName;

        private readonly IUserSessionService _userSessionService;

        public ContentDocumentDataService(
            IUserSessionService userSessionService)
        {
            _userSessionService = userSessionService;
        }

        public async Task<List<ContentDocument>> GetMyLibraryContentDocuments()
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var userId = _userSessionService.GetCurrentUserId();
                var smartQuery = "SELECT {"+ _soupName + ":_soup} FROM {"+ _soupName + "} WHERE {"+ _soupName + ":"+ContentDocument.OwnerIdIndexKey+"} = '"+userId+ "' AND {"+ _soupName + ":"+ContentDocument.PublishStatusIndexKey+"} ='R'";

                var querySpec = QuerySpec.BuildSmartQuerySpec(smartQuery, SfdcConfig.PageSize).RemoveLimit(globalStore);
                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContentDocument>(item[0].ToObject<JObject>().ToString()))
                        .ToList();
            });
        }

        public async Task<List<ContentDocument>> GetContentDocumentsByID15(IEnumerable<string> contentID15s)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec(_soupName, "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContentDocument>(item.ToString()))
                        .Where(c => contentID15s.Contains(c.Id15))
                        .ToList();
            });
        }

        public async Task<List<ContentDocument>> GetContentDocumentsByID(IEnumerable<string> contentIDs)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();

                var inCondition = string.Join(",", contentIDs.Select(s => $"'{s}'"));
                var smartQuery = "SELECT {" + _soupName + ":_soup} FROM {" + _soupName + "} WHERE {" + _soupName + ":Id} IN (" + inCondition + ")";
                var querySpec = QuerySpec.BuildSmartQuerySpec(smartQuery, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContentDocument>(item[0].ToObject<JObject>().ToString()))
                        .Where(c => contentIDs.Contains(c.Id))
                        .ToList();
            });
        }

        public async Task<List<ContentDocument>> GetAllContentDocuments()
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec(_soupName, "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<ContentDocument>(item.ToString()))
                        .ToList();
            });
        }
    }
}
