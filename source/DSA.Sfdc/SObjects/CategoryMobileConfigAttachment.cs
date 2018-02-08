using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;


namespace DSA.Sfdc.SObjects
{
    internal class CategoryMobileConfigAttachment : SObject
    {
        // Attributes
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("ParentId", SmartStoreType.SmartString)
        };

        // Properties
        public List<string> CmcIdList { get; set; }

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT " +
                            "ContentType,Id,IsPrivate,LastModifiedDate,Name,ParentId,BodyLength " +
                            "FROM " +
                            "Attachment " +
                            "{1}";

                return string.Format(query, Prefix, GenerateFilter());
            }
        }

        // CTOR
        internal CategoryMobileConfigAttachment(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }

            SoupName = "AttachmentMetadata";
            TempSoupName = "CategoryMobileConfigAttMeta";
        }

        // Methods
        internal IList<AttachmentMetadata> GetCategoryMobileAttachmentsFromSoup()
        {
            var querySpec = QuerySpec.BuildAllQuerySpec(TempSoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize);
            var results = Store.Query(querySpec, 0);
            var macList = results.Select(item => CustomPrefixJsonConvert.DeserializeObject<AttachmentMetadata>(item.ToString())).ToList();
            var endWithError = SaveMetadataToSoup(results);

            if (Store.HasSoup(TempSoupName) && !endWithError)
            {
                Store.DropSoup(TempSoupName);
            }

            return macList;
        }

        private string GenerateFilter()
        {
            if (CmcIdList.Count == 0)
            {
                return string.Empty;
            }
            else  // Handle IN creation
            {
                StringBuilder idList = new StringBuilder();

                idList.Append(string.Format("WHERE (parentid in (\'{0}\'", CmcIdList[0]));

                for (var i = 1; i < CmcIdList.Count; i++)
                {
                    idList.Append(string.Format(",\'{0}\'", CmcIdList[i]));
                }

                idList.Append("))");
                return idList.ToString();
            }
        }
    }
}
