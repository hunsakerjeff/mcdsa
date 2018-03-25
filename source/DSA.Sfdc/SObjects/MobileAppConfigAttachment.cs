using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;


namespace DSA.Sfdc.SObjects
{
    internal class MobileAppConfigAttachment : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("ParentId", SmartStoreType.SmartString)
        };

        // Properties
        public List<string> MacIdList { get; set; }

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT " +
                            "ContentType,Id,IsPrivate,LastModifiedDate,Name, ParentId, BodyLength " +
                            "FROM " +
                            "Attachment " +
                            "{1}";

                return string.Format(query, Prefix, GenerateFilter());
            }
        }

        // CTOR
        internal MobileAppConfigAttachment(SmartStore store) : base(store)
        {
            // Allocate new list
            MacIdList = new List<string>();

            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }

            SoupName = "AttachmentMetadata";
            TempSoupName = "MobileAppConfigAttMeta";
        }

        // Methods
        internal IList<AttachmentMetadata> GetMobileAppConfigAttachmentsFromSoup()
        {
            var querySpec = QuerySpec.BuildAllQuerySpec(TempSoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
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
            if (MacIdList.Count == 0)
            {
                return string.Empty;
            }
            else  // Handle IN creation
            {
                StringBuilder idList = new StringBuilder();

                idList.Append(string.Format("WHERE (parentid in (\'{0}\'", MacIdList[0]));

                for (var i = 1; i < MacIdList.Count; i++)
                {
                    idList.Append(string.Format(",\'{0}\'", MacIdList[i]));
                }

                idList.Append("))");
                return idList.ToString();
            }
        }
    }
}
