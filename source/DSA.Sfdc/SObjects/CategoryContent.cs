﻿using System.Collections.Generic;
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
    internal class CategoryContent : SObject
    {
        // Attributes
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec(Model.Models.CategoryContent.CategoryIdIndexKey, SmartStoreType.SmartString),
        };

        // Properties
        public List<string> CategoryIdList { get; set; }

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT " +
                            "Id,LastModifiedDate,{0}__Category__c,{0}__ContentId__c " +
                            "FROM " +
                            "{0}__Cat_Content_Junction__c " +
                            "{1}";  // WHERE/IN filter

                return string.Format(query, Prefix, GenerateFilter());
            }
        }

        // CTOR
        internal CategoryContent(SmartStore store) : base(store)
        {
            // Allocate new list
            CategoryIdList = new List<string>();

            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }
        }

        // Methods
        internal IList<Model.Models.CategoryContent> GetFromSoup()
        {
            RegisterSoup();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
            var results = Store.Query(querySpec, 0);

            return results.Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.CategoryContent>(item.ToString())).ToList();
        }

        // Assumption is Category Id list (from CAT) is set before call is made for proper filtering
        internal async Task<IList<Model.Models.CategoryContent>> GetFromSoql(SyncManager syncManager)
        {
            List<Model.Models.CategoryContent> results = new List<Model.Models.CategoryContent>();
            var target = new SoqlSyncDownTarget(SoqlQuery);

            var _results = await target.StartFetch(syncManager, -1);
            while (_results != null && _results.Count > 0)
            {
                results.AddRange(_results.Select(x => x.ToObject<Model.Models.CategoryContent>()).ToList());
                _results = await target.ContinueFetch(syncManager);
            }
            return results;
        }

        internal List<string> GetIds()
        {
            var catConList = GetFromSoup().ToList();
            List<string> catConIdList = new List<string>();

            // Parse the Cat Con List and grab the Ids
            foreach (var catConModel in catConList)
            {
                catConIdList.Add(catConModel.Id);
            }

            return catConIdList;
        }

        internal List<string> GetContentIds()
        {
            // Local Variables
            var categoryContentList = GetFromSoup().ToList();
            HashSet<string> contentIdSet = new HashSet<string>();

            // Parse the CategoryContent List down to an Content ID list (remove duplicates)
            foreach (var categoryContentModel in categoryContentList)
            {
                contentIdSet.Add(categoryContentModel.ContentId);
            }
            return contentIdSet.ToList();
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
                idList.Append(string.Format("(\'{0}\'", CategoryIdList[0]));

                for (var i = 1; i < CategoryIdList.Count; i++)
                {
                    idList.Append(string.Format(",\'{0}\'", CategoryIdList[i]));
                }

                idList.Append("))");
                return string.Format("WHERE ({0}__Category__c in {1}", Prefix, idList.ToString());
            }
        }
    }
}
