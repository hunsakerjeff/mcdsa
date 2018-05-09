using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;
using System.Threading.Tasks;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;

namespace DSA.Sfdc.SObjects
{
    internal class Category : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
        };

        // Attributes
        private User _currentUserPredicate;

        // Properties
        public List<string> CategoryIdList { get; set; }

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT " +
                            "Id,LastModifiedDate,{0}__Description__c,{0}__GalleryAttachmentId__c,{0}__Is_Parent_Category__c,{0}__Is_Parent__c," +
                            "{0}__Is_Top_Level__c,{0}__Language__c,{0}__Order__c,{0}__Parent_Category__c,Name, " +
                            "(SELECT Id FROM Attachments ORDER BY LastModifiedDate DESC limit 1) " +
                            "FROM " +
                            "{0}__Category__c " +
                            "WHERE " +
                            "({0}__Is_Top_Level__c = 1.0 or {0}__Is_Top_Level__c = 0.0)" +
                            "{1}";  // IN filter

                return string.Format(query, Prefix, GenerateFilter());
            }
        }

        // CTOR
        internal Category(SmartStore store, User currentUserPredicate) : base(store)
        {
            // Allocate new list
            CategoryIdList = new List<string>();

            if (currentUserPredicate == null || currentUserPredicate is NullUser)
            {
                throw new ArgumentNullException(nameof(User), "Parameter currentUserPredicate must be concrete not null object");
            }

            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }

            _currentUserPredicate = currentUserPredicate;
        }

        // Methods
        internal IList<Model.Models.Category> GetFromSoup()
        {
            RegisterSoup();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
            var results = Store.Query(querySpec, 0);

            return results.Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.Category>(item.ToString())).ToList();
        }

        // Assumption is Category Id list (from CMC) is set before call is made for proper filtering
        internal async Task<IList<Model.Models.Category>> GetFromSoql(SyncManager syncManager)
        {
            List<Model.Models.Category> results = new List<Model.Models.Category>();
            var target = new SoqlSyncDownTarget(SoqlQuery);

            // Sync to JSON
            var _results = await target.StartFetch(syncManager, -1);
            while (_results != null && _results.Count > 0)
            {
                results.AddRange(_results.Select(x => x.ToObject<Model.Models.Category>()).ToList());
                _results = await target.ContinueFetch(syncManager);
            }
            return results;
        }

        internal List<string> GetIds()
        {
            var catList = GetFromSoup().ToList();
            List<string> categoryIdList = new List<string>();

            // Parse the Category List and grab the Ids
            foreach (var catModel in catList)
            {
                categoryIdList.Add(catModel.Id);
            }

            return categoryIdList;
        }

        private string GenerateFilter()
        {
            if (CategoryIdList.Count == 0)
            {
                return string.Empty;
            }
            else  // Handle IN creation
            {
                StringBuilder idList = new StringBuilder();
                idList.Append(string.Format(" AND (Id in (\'{0}\'", CategoryIdList[0]));

                for (var i = 1; i < CategoryIdList.Count; i++)
                {
                    idList.Append(string.Format(",\'{0}\'", CategoryIdList[i]));
                }

                idList.Append("))");
                return idList.ToString();
            }
        }
    }
}
