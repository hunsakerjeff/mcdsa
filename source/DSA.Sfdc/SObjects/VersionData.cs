using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class VersionData : SObject
    {
        internal VersionData(SmartStore store, Model.Models.ContentDocument docMetadata) : base(store)
        {
            LatestPublishedVersionId = docMetadata.LatestPublishedVersionId;
        }
    }
}