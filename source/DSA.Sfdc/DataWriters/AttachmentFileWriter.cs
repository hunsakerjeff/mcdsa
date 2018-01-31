using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;
using DSA.Model.Models;
using DSA.Sfdc.Storage;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Net;

namespace DSA.Sfdc.DataWriters
{
    public class AttachmentFileWriter : IHttpDataWriter
    {
        private readonly AttachmentMetadata _meta;
        private readonly long _syncId;

        public AttachmentFileWriter(AttachmentMetadata attMeta, long syncId)
        {
            if (attMeta == null) { throw new NullReferenceException("Parameter attMeta is null"); }
            _syncId = syncId;
            _meta = attMeta;
        }
        
        public async Task PushToStreamAsync(
            Func<IOutputStream, IAsyncOperationWithProgress<ulong, ulong>> writeToStreamAsync, 
            IHttpContent content)
        {
            
            if (content == null) { throw new NullReferenceException("Parameter content is null");}
            if (writeToStreamAsync == null) { throw new NullReferenceException("Parameter writeToStreamAsync is null"); }

            var attFolderMgr = AttachmentsFolder.Instance;

            var attFolder = await attFolderMgr.CreateOrGetFolderForAttachmentId(_meta.Id, _syncId);

            try
            {
                StorageFile newFile = await attFolder.CreateFileAsync(_meta.Name, CreationCollisionOption.ReplaceExisting);
                using (var sfw = await newFile.OpenStreamForWriteAsync())
                {
                    using (var outStream = sfw.AsOutputStream())
                    {
                        ulong x = await content.WriteToStreamAsync(outStream);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                Debug.WriteLine($"Exception Opening Attachment File For Write { _meta.Name } ");
            }
        }
    }
}
