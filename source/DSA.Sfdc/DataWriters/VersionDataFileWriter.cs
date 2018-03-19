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
        private static byte[] buffer1 = new byte[65536];
        private static byte[] buffer2 = new byte[65536];

        public VersionDataFileWriter(ContentDocument docMeta, long syncId)
        {
            if (docMeta == null) { throw new NullReferenceException("Parameter docMeta is null"); }

            _syncId = syncId;
            _meta = docMeta;
        }

        //public async Task PushToStreamAsync(Func<IOutputStream, IAsyncOperationWithProgress<ulong, ulong>> writeToStreamAsync, IHttpContent content)
        //{

        //    if (content == null)
        //    {
        //        throw new NullReferenceException("Parameter content is null");
        //    }
        //    if (writeToStreamAsync == null)
        //    {
        //        throw new NullReferenceException("Parameter writeToStreamAsync is null");
        //    }


        //    var versionDataFolder = VersionDataFolder.Instance;
        //    var attFolder = await versionDataFolder.CreateOrGetFolderForVersionDataId(_meta.Id, _syncId);
        //    var filename = Path.GetFileName(_meta.PathOnClient);

        //    try
        //    {
        //        StorageFile newFile = await attFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

        //        using (var fileStream = await newFile.OpenStreamForWriteAsync())
        //        {
        //            using (var outStream = fileStream.AsOutputStream())
        //            {
        //                using (Stream responseStream = (await content.ReadAsInputStreamAsync()).AsStreamForRead())
        //                {
        //                    int currentRead = 0;
        //                    byte[] currentBuffer = buffer1;
        //                    byte[] backBuffer = buffer2;
        //                    byte[] tempBuffer = null;

        //                    // first read
        //                    currentRead = await responseStream.ReadAsync(currentBuffer, 0, currentBuffer.Length);

        //                    do
        //                    {
        //                        //  Write Buffer 
        //                        var writeTask = fileStream.WriteAsync(currentBuffer, 0, currentRead);

        //                        // Read Network
        //                        var readTask = responseStream.ReadAsync(backBuffer, 0, backBuffer.Length);

        //                        // Wait for completion
        //                        await Task.WhenAll(writeTask, readTask);

        //                        // Swap Buffers
        //                        tempBuffer = currentBuffer;
        //                        currentBuffer = backBuffer;
        //                        backBuffer = tempBuffer;

        //                        // Update indices
        //                        currentRead = readTask.Result;
        //                    }
        //                    while (currentRead != 0);

        //                    // Flush the rest of the data to disk
        //                    await fileStream.FlushAsync();
        //                }
        //            }
        //        }

        //        await ThumbnailUtil.SaveThumbnail(newFile, attFolder, _meta);
        //    }
        //    catch (UnauthorizedAccessException uae)
        //    {
        //        PlatformAdapter.SendToCustomLogger(uae, LoggingLevel.Error);
        //        Debug.WriteLine($"Exception Opening Content Version File For Write {filename} ");
        //    }
        //}

        public async Task PushToStreamAsync(Func<IOutputStream, IAsyncOperationWithProgress<ulong, ulong>> writeToStreamAsync, IHttpContent content)
        {

            if (content == null)
            {
                throw new NullReferenceException("Parameter content is null");
            }
            if (writeToStreamAsync == null)
            {
                throw new NullReferenceException("Parameter writeToStreamAsync is null");
            }


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
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                Debug.WriteLine($"Exception Opening Content Version File For Write {filename} ");
            }
        }
    }
}
