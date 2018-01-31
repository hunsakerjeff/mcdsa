using System;
using DSA.Model.Models;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class Category : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
        };

        private User _currentUserPredicate;

        internal override string SoqlQuery => string.Format("SELECT " +
                                              "Id," +
                                              "LastModifiedDate," +
                                              "{0}__Description__c," +
                                              "{0}__GalleryAttachmentId__c," +
                                              "{0}__Is_Parent_Category__c," +
                                              "{0}__Is_Parent__c," +
                                              "{0}__Is_Top_Level__c," +
                                              "{0}__Language__c," +
                                              "{0}__Order__c," +
                                              "{0}__Parent_Category__c," +
                                              "Name, " +
                                              "(select id from Attachments order by LastModifiedDate desc limit 1) " +
                                              "FROM {0}__Category__c " +
                                              "WHERE {0}__Is_Top_Level__c = 1.0 " +
                                              "or {0}__Is_Top_Level__c = 0.0", Prefix);

        internal Category(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser)
            {
                throw new ArgumentNullException(nameof(User), "Parameter currentUserPredicate must be concrete not null object");
            }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }
    }
}