using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using DSA.Common;
using DSA.Common.Utils;
using DSA.Model.Dto;

namespace DSA.Shell.Common
{
    public static class ThumbnailIconResolver
    {
        public static async Task<ImageSource> GetIconAsync(MediaLink media, int widthPixels)
        {
            if (!string.IsNullOrEmpty(media.ContentThumbnail))
            {
                var tmb = await ImageUtil.GetImageSouceAndRelease(media.ContentThumbnail, widthPixels);
                if (tmb != null)
                {
                    return tmb;
                }
            }
            if (media.Type == MediaType.Image
                || media.Type == MediaType.Video
                || media.Type == MediaType.MP4
                || media.Type == MediaType.PDF)
            {
                var tmb = ImageUtil.GetImageSouce(media.Thumbnail);
                if (tmb != null)
                {
                    return tmb;
                }
            }

            return TypeIconResolver.GetIcon(media.Type);
        }

        public static ImageSource GetIcon(MediaLink media)
        {
            if (!string.IsNullOrEmpty(media.ContentThumbnail))
            {
                var tmb = ImageUtil.GetImageSouce(media.ContentThumbnail);
                if (tmb != null)
                {
                    return tmb;
                }
            }

            if (media.Type == MediaType.Image
              || media.Type == MediaType.Video
              || media.Type == MediaType.MP4
              || media.Type == MediaType.PDF)
            {
                var tmb = ImageUtil.GetImageSouce(media.Thumbnail);
                if(tmb != null)
                {
                    return tmb;
                }
            }

            return TypeIconResolver.GetIcon(media.Type);
        }
    }
}
