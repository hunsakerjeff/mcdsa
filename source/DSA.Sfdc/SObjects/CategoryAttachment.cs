using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class CategoryAttachment : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("ParentId", SmartStoreType.SmartString)
        };

        internal override string SoqlQuery => string.Format("SELECT ContentType,Id,IsPrivate,LastModifiedDate,Name, ParentId, BodyLength " +
                                              "FROM Attachment where parentid in (SELECT Id FROM {0}__Category__c " +
                                              "WHERE {0}__Is_Top_Level__c = 1.0 or ({0}__Is_Top_Level__c = 0.0))", Prefix);

        internal CategoryAttachment(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);

            SoupName = "AttachmentMetadata";
            TempSoupName = "CategoryAttMeta";
        }

        internal IList<AttachmentMetadata> GetCategoryAttachmentsFromSoup()
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
    }
}