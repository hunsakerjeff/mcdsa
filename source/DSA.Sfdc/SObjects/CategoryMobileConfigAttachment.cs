using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class CategoryMobileConfigAttachment : SObject
    {
        private readonly User _currentUserPredicate;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("ParentId", SmartStoreType.SmartString)
        };

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT ContentType," +
                                "Id," +
                                "IsPrivate," +
                                "LastModifiedDate," +
                                "Name, " +
                                "ParentId, " +
                                "BodyLength " +
                            "FROM Attachment " +
                            "WHERE parentid in " +
                                "(select id " +
                                "FROM {0}__CategoryMobileConfig__c " +
                                "WHERE {0}__MobileAppConfigurationId__r.{0}__Profiles__c " +
                                "INCLUDES (\'{1}\'))"; 
                
                return string.Format(query, Prefix, _currentUserPredicate.ProfileId);
            }
        }

        internal CategoryMobileConfigAttachment(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser) { throw new ArgumentNullException("Parameter currentUserPredicate must be concrete not null object"); }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);

            SoupName = "AttachmentMetadata";
            TempSoupName = "CategoryMobileConfigAttMeta";
        }

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

    }
}