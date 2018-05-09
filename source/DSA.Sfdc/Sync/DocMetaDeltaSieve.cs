using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using DSA.Sfdc.Storage;
using Salesforce.SDK.SmartStore.Store;
using CategoryContent = DSA.Sfdc.SObjects.CategoryContent;
using ContentDocument = DSA.Model.Models.ContentDocument;

namespace DSA.Sfdc.Sync
{
    public class DocMetaDeltaSieve : IResultSieve<IList<ContentDocument>>
    {
        private readonly SmartStore _store;
        private readonly User _currentUser;

        public DocMetaDeltaSieve(SmartStore store, User currentUser)
        {
            if (store == null || currentUser == null)
            {
                throw new SyncException("store and currentUser params are required to filtered data.");
            } 

            _store = store;
            _currentUser = currentUser;
        }

        public IList<ContentDocument> GetFilteredResult(IList<ContentDocument> resultUnfiltered)
        {
            return FilterUsedDocuments(resultUnfiltered, _currentUser);
        }

        public IList<ContentDocument> GetFilteredResult(IList<ContentDocument> resultUnfiltered, IList<Model.Models.CategoryContent> categoryContents)
        {
            return FilterUsedDocuments(resultUnfiltered, _currentUser, categoryContents);
        }

        private IList<ContentDocument> FilterUsedDocuments(IList<ContentDocument> docMetadata, User currentUser, IList<Model.Models.CategoryContent> categoryContents = null)
        {
            categoryContents = categoryContents ?? new CategoryContent(_store).GetFromSoup();
            List <string> documentsInCategories = categoryContents.Select(cc => cc.ContentId15).ToList();
            
            var docsUserOrPrivateLib =  docMetadata.Where(cd => (cd.OwnerId == currentUser.Id && cd.PublishStatus == "R") || documentsInCategories.Contains(cd.Id15)).ToList();

            var docsLocal = SuccessfulSyncState.Instance.GetAllVersionedsSyncsFromSoupIndexedByDocId();
            
            var delta = new List<ContentDocument>();

            foreach (var item in docsUserOrPrivateLib)
            {
                SuccessfulSync ss = null;
                var isAlreadyStoredLocally = docsLocal.TryGetValue(item.Id, out ss);

                if ((!isAlreadyStoredLocally || item.LatestPublishedVersionId == ss.DocVersionId) && isAlreadyStoredLocally)
                    continue;

                delta.Add(item);
                if (isAlreadyStoredLocally && item.LatestPublishedVersionId != ss.DocVersionId)
                {
                    var versionDataFolder = VersionDataFolder.Instance;
                    versionDataFolder.DeleteVersionDataIdWithContent(item.Id, ss.SyncId);
                    //TODO: remove old version
                }
            }

            return delta;
        }
    }
}
