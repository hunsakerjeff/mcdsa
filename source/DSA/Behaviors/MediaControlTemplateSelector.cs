using DSA.Model.Dto;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DSA.Util
{
    public class MediaControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PdfTemplate { get; set; }
        public DataTemplate MoviewTemplate { get; set; }
        public DataTemplate WebViewTemplate { get; set; }
        public DataTemplate UrlWebViewTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate OtherTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var media = item as MediaLink;
            if(media == null)
            {
                return OtherTemplate;
            }

            switch(media.Type)
            {

                case MediaType.Html:
                    return WebViewTemplate;
                case MediaType.MP4:
                case MediaType.Video:
                case MediaType.Audio:
                    return MoviewTemplate;
                case MediaType.PDF:
                    return PdfTemplate;
                case MediaType.Image:
                    return ImageTemplate;
                case MediaType.Url:
                    return UrlWebViewTemplate;
                case MediaType.Unknow:
                default:
                    return OtherTemplate;
            }
        }

    }
}
