using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;
using System.Text;

namespace DSA.Sfdc.SObjects
{
    /// <summary>
    /// ContentDocument
    /// https://developer.salesforce.com/docs/atlas.en-us.api.meta/api/sforce_api_objects_contentdocument.htm
    /// </summary>
    internal class ContentDocument : SObject
    {
        // Attributes
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec(Model.Models.ContentDocument.PublishStatusIndexKey, SmartStoreType.SmartString),
            new IndexSpec(Model.Models.ContentDocument.OwnerIdIndexKey, SmartStoreType.SmartString)
        };

        // Properties
        public List<string> ContentIdList { get; set; }

        internal override string SoqlQuery
        {
            get
            {
                //var productTypeQuery = Prefix != "ModelM"
                //    ? "LatestPublishedVersion.{0}__Product_Type__c, LatestPublishedVersion.{0}__Asset_Type__c, "
                //    : "";
                //var contentThumbnailQuery = Prefix != "ModelM"
                //    ? "LatestPublishedVersion.{0}__DSA_Content_Thumbnail__c, LatestPublishedVersion.{0}__DSA_Content_Thumbnail__r.Name, "
                //    : "";
                var query = "SELECT Id,LastModifiedDate," +
                            "LatestPublishedVersionId," +
                            "ContentSize," +
                            "LatestPublishedVersion.PathOnClient, " +
                            "LatestPublishedVersion.{0}__Available_Offline__c, " +
                            "LatestPublishedVersion.{0}__Internal_Document__c, " +
                            "LatestPublishedVersion.{0}__Document_Type__c, " +
                            "LatestPublishedVersion.FeaturedContentBoost, " +
                            "LatestPublishedVersion.FeaturedContentDate, " +
                            "LatestPublishedVersion.{0}__IsFeatured__c, " +
                            "LatestPublishedVersion.{0}__Content_Owner__r.Name, " +
                            "LatestPublishedVersion.{0}__Content_Last_Updated__c, " +
                            "LatestPublishedVersion.LastModifiedDate, " +
                            "LatestPublishedVersion.ContentUrl, " +
                            "LatestPublishedVersion.TagCsv, " +
                            "LatestPublishedVersion.ContentSize, " +
                            "LatestPublishedVersion.Last_Reviewed_Recertified_Date_View__c," +
                            //productTypeQuery +
                            //contentThumbnailQuery +
                            "FileType, " +
                            "Title, " +
                            "LatestPublishedVersion.Description, " +
                            "PublishStatus, " +
                            "OwnerId " +
                            "FROM " +
                            "ContentDocument " +
                            "WHERE LatestPublishedVersion.{0}__Available_Offline__c = true AND (PublishStatus = 'R' OR (PublishStatus='P'{1}))";

                return string.Format(query, Prefix, GenerateFilter());
            }
        }

        // CTOR
        internal ContentDocument(SmartStore store) : base(store)
        {
            // Allocate new list
            ContentIdList = new List<string>();

            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }
        }

        // Methods
        /// <summary>
        /// Get content documents from local db
        /// </summary>
        internal IList<Model.Models.ContentDocument> GetFromSoup()
        {
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
            var results = Store.Query(querySpec, 0);
            return results.Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.ContentDocument>(item.ToString())).ToList();
        }

        /// <summary>
        /// Get content documents from sfdc
        /// </summary>
        internal async Task<IList<Model.Models.ContentDocument>> GetFromSoql(SyncManager syncManager)
        {
            var results = new List<Model.Models.ContentDocument>();
            var target = new SoqlSyncDownTarget(SoqlQuery);

            var page = await target.StartFetch(syncManager, -1);
            while (page != null && page.Count > 0)
            {
                results.AddRange(page.Select(x => x.ToObject<Model.Models.ContentDocument>()).ToList());
                page = await target.ContinueFetch(syncManager);
            }
            return results;
        }

        private string GenerateFilter()
        {
            if (ContentIdList.Count == 0)
            {
                return string.Empty;
            }
            else  // Handle IN creation
            {
                StringBuilder idList = new StringBuilder();

                idList.Append(string.Format(" AND (Id in (\'{0}\'", ContentIdList[0]));

                for (var i = 1; i < ContentIdList.Count; i++)
                {
                    idList.Append(string.Format(",\'{0}\'", ContentIdList[i]));
                }

                idList.Append("))");
                return idList.ToString();
            }
        }
    }
}
