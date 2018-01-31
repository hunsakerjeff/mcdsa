using System;
using DSA.Model.Models;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class Contact : SObject
    {
        private readonly User _currentUserPredicate;

        internal Contact(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser)
            {
                throw new ArgumentNullException(nameof(User), "Parameter currentUserPredicate must be concrete not null object");
            }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null)
                AddIndexSpecItems(IndexedFieldsForSObjects);

            if (!Store.HasSoup("RecentContact"))
            {
                Store.RegisterSoup("RecentContact", IndexSpecs);
            }
        }

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString)
        };

        internal override string SoqlQuery => $"SELECT Id, Email, FirstName, LastName, Account.Name FROM Contact WHERE ownerId = \'{_currentUserPredicate.Id}\'";
    }
}
