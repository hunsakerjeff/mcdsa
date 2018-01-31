using DSA.Model.Models;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class AttachmentBody : SObject
    {
        private AttachmentMetadata _attMetadata;
        internal AttachmentBody(SmartStore store, AttachmentMetadata attMetadata) : base(store)
        {
            _attMetadata = attMetadata;
            AttachmentId = attMetadata.Id;
        }
    }
}