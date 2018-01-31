using System;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using DSA.Model.Dto;
using DSA.Model.Models;
using DSA.Model.Util;
using Salesforce.SDK.Adaptation;

namespace DSA.Sfdc.Util
{
    class ThumbnailUtil
    {
        private const string ThumbnailName = "thumbnail.png";

        public static async Task SaveThumbnail(StorageFile file, StorageFolder folder, ContentDocument document)
        {
            var type = MediaTypeResolver.ResolveType(document.FileType, document.PathOnClient);
            switch (type)
            {
                case MediaType.Image:
                case MediaType.MP4:
                case MediaType.Video:
                    await SaveThumbnail(file, folder, ThumbnailMode.PicturesView);
                    break;
                case MediaType.PDF:
                    await SavePDFThumbnail(file, folder);
                    break;
                default:
                    return;
            }
        }

        private static async Task SavePDFThumbnail(StorageFile file, StorageFolder folder)
        {
            PdfDocument pdfDocument;
            try
            {
                pdfDocument = await PdfDocument.LoadFromFileAsync(file);
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                return;
            }
            if (pdfDocument == null)
            {
                return;
            }

            var currentPage = pdfDocument.GetPage(0);
            var destinationFile = await folder.CreateFileAsync(ThumbnailName, CreationCollisionOption.GenerateUniqueName);
            using (var strm = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await currentPage.RenderToStreamAsync(strm, new PdfPageRenderOptions() { DestinationHeight = 300, DestinationWidth = 263 });
            }
        }

        private static async Task SaveThumbnail(StorageFile file, StorageFolder folder, ThumbnailMode mode)
        {
            using (StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(mode, 100))
            {
                if (thumbnail != null && thumbnail.Type == ThumbnailType.Image)
                {
                    var destinationFile = await folder.CreateFileAsync(ThumbnailName, CreationCollisionOption.GenerateUniqueName);
                    using (var strm = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var buffer = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(thumbnail.Size));
                        IBuffer iBuf = await thumbnail.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                        await strm.WriteAsync(iBuf);
                    }
                }
            }
        }

    }
}
