using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace DSA.Common.Utils
{
    /// <summary>
    /// Image helper
    /// </summary>
    public static class ImageUtil
    {
        public static async Task<Tuple<string,ImageSource>> GetImageSouceAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
        
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
            if(file == null)
            {
                return null;
            }

            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                return Tuple.Create<string, ImageSource>(path, bitmapImage);
            }
        }                

        public static ImageSource GetImageSouce(string path)
        {
            return string.IsNullOrEmpty(path) == false
                    ? new BitmapImage(new Uri(path))
                    : null;
        }

        // GetImageSouce (above) locks file from writing during sync
        public static async Task<ImageSource> GetImageSouceAndRelease(string path, int widthPixels)
        {
            ImageSource imageSource = null;
            if (string.IsNullOrEmpty(path) == false)
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
                if (file != null)
                {
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage image = new BitmapImage();
                        image.DecodePixelWidth = widthPixels;
                        image.SetSource(stream);
                        imageSource = image;
                    }
                }
            }
            return imageSource;
        }

        public static string GetPathFromAttachment(string attachmentId, string syncId, string name)
        {
            return $"ms-appdata:///local/Attachments/{attachmentId}/{syncId}/{name}";
        }
    }
}
