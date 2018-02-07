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
using DSA.Sfdc.Util;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Net;

namespace DSA.Sfdc.DataWriters
{
    public class VersionDataFileWriter : IHttpDataWriter
    {
        private readonly ContentDocument _meta;
        private readonly long _syncId;

        public VersionDataFileWriter(ContentDocument docMeta, long syncId)
        {
            if (docMeta == null) { throw new NullReferenceException("Parameter docMeta is null"); }

            _syncId = syncId;
            _meta = docMeta;
        }
        
        public async Task PushToStreamAsync(
            Func<IOutputStream, IAsyncOperationWithProgress<ulong, ulong>> writeToStreamAsync, 
            IHttpContent content)
        {
            
            if (content == null) { throw new NullReferenceException("Parameter content is null");}
            if (writeToStreamAsync == null) { throw new NullReferenceException("Parameter writeToStreamAsync is null"); }


            var versionDataFolder = VersionDataFolder.Instance;

            var attFolder = await versionDataFolder.CreateOrGetFolderForVersionDataId(_meta.Id, _syncId);
            var filename = Path.GetFileName(_meta.PathOnClient);

            try
            {
                StorageFile newFile = await attFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                using (var sfw = await newFile.OpenStreamForWriteAsync())
                {
                    using (var outStream = sfw.AsOutputStream())
                    {
                        ulong x = await content.WriteToStreamAsync(outStream);
                    }
                }
                await ThumbnailUtil.SaveThumbnail(newFile, attFolder, _meta);
            }
            catch (UnauthorizedAccessException uae)
            {
                PlatformAdapter.SendToCustomLogger(uae, LoggingLevel.Error);
                Debug.WriteLine($"Exception Opening Content Version File For Write {filename} ");
            }
        }
    }
}
