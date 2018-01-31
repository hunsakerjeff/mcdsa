using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DSA.Model.Dto;

namespace DSA.Shell.Common
{
    public static class TypeIconResolver
    {
        private static readonly Dictionary<MediaType, Uri> IconUriDictionary = new Dictionary<MediaType, Uri>
        {
            {MediaType.MP4,    new Uri("ms-appx:///Assets/Icons/mp4_thumbnail@2x.png") },
            {MediaType.Video,  new Uri("ms-appx:///Assets/Icons/video_thumbnail@2x.png") },
            {MediaType.AI,    new Uri("ms-appx:///Assets/Icons/ai_thumbnail@2x.png") },
            {MediaType.Audio,    new Uri("ms-appx:///Assets/Icons/audio_thumbnail@2x.png") },
            {MediaType.Csv,    new Uri("ms-appx:///Assets/Icons/csv_thumbnail@2x.png") },
            {MediaType.Esp,    new Uri("ms-appx:///Assets/Icons/esp_thumbnail@2x.png") },
            {MediaType.Excel,  new Uri("ms-appx:///Assets/Icons/excel_thumbnail@2x.png") },
            {MediaType.Html,   new Uri("ms-appx:///Assets/Icons/html_thumbnail@2x.png") },
            {MediaType.HtmlFile,   new Uri("ms-appx:///Assets/Icons/html_thumbnail@2x.png") },
            {MediaType.Image,  new Uri("ms-appx:///Assets/Icons/image_thumbnail@2x.png") },
            {MediaType.Keynote,  new Uri("ms-appx:///Assets/Icons/keynote_thumbnail@2x.png") },
            {MediaType.Pages,  new Uri("ms-appx:///Assets/Icons/pages_thumbnail@2x.png") },
            {MediaType.PDF,    new Uri("ms-appx:///Assets/Icons/pdf_thumbnail@2x.png") },
            {MediaType.PowerPoint, new Uri("ms-appx:///Assets/Icons/ppt_thumbnail@2x.png") },
            {MediaType.PSD,    new Uri("ms-appx:///Assets/Icons/psd_thumbnail@2x.png") },
            {MediaType.Rtf,    new Uri("ms-appx:///Assets/Icons/rtf_thumbnail@2x.png") },
            {MediaType.Txt,    new Uri("ms-appx:///Assets/Icons/txt_thumbnail@2x.png") },
            {MediaType.Visio,  new Uri("ms-appx:///Assets/Icons/visio_thumbnail@2x.png") },
            {MediaType.Word,   new Uri("ms-appx:///Assets/Icons/word_thumbnail@2x.png") },
            {MediaType.Xml,    new Uri("ms-appx:///Assets/Icons/xml_thumbnail@2x.png") },
            {MediaType.Url,    new Uri("ms-appx:///Assets/Icons/url_thumbnail@2x.png") },
            {MediaType.Unknow, new Uri("ms-appx:///Assets/Icons/unknown_thumbnail@2x.png") },
        };

        private static readonly Dictionary<MediaType, ImageSource> IconImageSourceDictionary;
        private static readonly Dictionary<MediaType, RandomAccessStreamReference> StreamReferenceDictionary;

        static TypeIconResolver()
        {
            IconImageSourceDictionary = IconUriDictionary
                                                .Select(kp => new { Key = kp.Key, Value = new BitmapImage(kp.Value) as ImageSource })
                                                .ToDictionary(k => k.Key, k => k.Value);

            StreamReferenceDictionary = IconUriDictionary
                                    .Select(kp => new { Key = kp.Key, Value = RandomAccessStreamReference.CreateFromUri(kp.Value) })
                                    .ToDictionary(k => k.Key, k => k.Value);
        }

        public static ImageSource GetIcon(MediaType mediaType)
        {
            return IconImageSourceDictionary.ContainsKey(mediaType)
                        ? IconImageSourceDictionary[mediaType]
                        : IconImageSourceDictionary[MediaType.Unknow];
        }

        public static  RandomAccessStreamReference GetIconStreamReference(MediaType mediaType)
        {
            return StreamReferenceDictionary.ContainsKey(mediaType)
                       ? StreamReferenceDictionary[mediaType]
                       : StreamReferenceDictionary[MediaType.Unknow];
        }
    }
}
