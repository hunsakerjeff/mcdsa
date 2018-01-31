using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using Salesforce.SDK.SmartStore.Store;
using CategoryContent = DSA.Sfdc.SObjects.CategoryContent;
using ContentDocument = DSA.Model.Models.ContentDocument;

namespace DSA.Sfdc.Sync
{
    public class DocMetaByOwnershipAndUsageInCategoriesSieve : IResultSieve<IList<ContentDocument>>
    {
        private readonly SmartStore _store;
        private readonly User _currentUser;

        public DocMetaByOwnershipAndUsageInCategoriesSieve(SmartStore store, User currentUser)
        {
            if (store == null || currentUser == null)
            {
                throw new SyncException("store and currentUser params are required to filted data.");
            } 

            _store = store;
            _currentUser = currentUser;
        }

        public IList<ContentDocument> GetFilteredResult(IList<ContentDocument> resultUnfiltered)
        {
            return FilterUsedDocuments(resultUnfiltered, _currentUser);
        }

        private IList<ContentDocument> FilterUsedDocuments(IList<ContentDocument> docMetadata, User currentUser)
        {
            var documentsInCategories = new CategoryContent(_store).GetAll().Select(cc => cc.ContentId15).ToList();
            return docMetadata
                .Where(cd => (cd.OwnerId == currentUser.Id && cd.PublishStatus == "R") || documentsInCategories.Contains(cd.Id15)).ToList();
        }
    }
}
