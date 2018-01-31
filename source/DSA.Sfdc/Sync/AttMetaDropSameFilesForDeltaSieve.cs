using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.Sync
{
    public class AttMetaDropSameFilesForDeltaSieve : IResultSieve<IList<AttachmentMetadata>>
    {
        private readonly SmartStore _store;

        public AttMetaDropSameFilesForDeltaSieve(SmartStore store)
        {
            if (store == null)
            {
                throw new SyncException("store params is required to filted data.");
            } 
            _store = store;
        }

        public IList<AttachmentMetadata> GetFilteredResult(IList<AttachmentMetadata> resultUnfiltered)
        {
            var attLocal = SuccessfulSyncState.Instance.GetAllSuccessfulSyncsFromSoup().Select(ss => ss.TransactionItemType).ToList();
            
            return resultUnfiltered.Where(ru => !attLocal.Contains(ru.Id)).ToList();
        }
    }
}
