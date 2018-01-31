using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Common.Extensions;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Data.Services.API
{
    public class AttachmentDataService : IAttachmentDataService
    {
        public static string AttachmentSoupName = "AttachmentMetadata";
        public static string CategoryAttachmentSoupName = "CategoryAttMeta";

        public async Task<List<AttachmentMetadata>> GetAttachmentsMetadata(IEnumerable<string> attachmentIDs)
        {
            return await Task.Factory.StartNew(() =>
            {
                return GetAttachments(AttachmentSoupName, attachmentIDs)
                         .AppendList(GetAttachments(CategoryAttachmentSoupName, attachmentIDs))
                         .ToList();
            });
        }

        private static List<AttachmentMetadata> GetAttachments(string soupName, IEnumerable<string> attachmentIDs)
        {
            var inCondition = string.Join(",", attachmentIDs.Select(s => $"'{s}'"));
            var smartQuery = "SELECT {" + soupName + ":_soup} FROM {" + soupName + "}  WHERE {" + soupName + ":Id} IN (" + inCondition + ")";

            var globalStore = SmartStore.GetGlobalSmartStore();
            var querySpec = QuerySpec.BuildSmartQuerySpec(smartQuery, SfdcConfig.PageSize).RemoveLimit(globalStore);
            
            return globalStore
                        .Query(querySpec, 0)
                        .Select(item => JsonConvert.DeserializeObject<AttachmentMetadata>(item[0].ToObject<JObject>().ToString()))
                        .ToList();
        }
    }
}
