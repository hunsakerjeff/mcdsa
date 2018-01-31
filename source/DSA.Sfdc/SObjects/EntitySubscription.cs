using System;
using DSA.Model.Models;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class EntitySubscription : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("ParentId", SmartStoreType.SmartString),
        };

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT Id," +
                            "ParentId " +
                            "FROM EntitySubscription " +
                            "WHERE SubscriberId = '{1}' LIMIT 1000";//Limit is required by salesforce for the EntitySubscription object

                return string.Format(query, Prefix, _currentUserPredicate.Id);

            }
        }

        private readonly User _currentUserPredicate;

        internal EntitySubscription(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser) { throw new ArgumentNullException("Parameter currentUserPredicate must be concrete not null object"); }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }
    }
}