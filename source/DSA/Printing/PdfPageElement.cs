using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace DSA.Common

{
    public class PdfPageElement
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        private Image _image;

        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public PdfPage Page { get; set; }

        public Image Image 
        {
            get { return _image; }
        }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public PdfPageElement(PdfPage page)
        {
            Page = page;
        }

        public async Task Initialize()
        {
            _image = await CreateImage();
        }

        public UIElement GetPageInTargetResolution(double X, double Y)
        {
            return null;
        }

        public UIElement GetPreviewPage(double X, double Y)
        {
            return null;
        }

        public UIElement GetPrintPage(double X, double Y)
        {
            return null;
        }

        private async Task<Image> CreateImage()
        {
            // Render the PDF Page to a memory Stream
            var stream = new InMemoryRandomAccessStream();
            await Page.RenderToStreamAsync(stream);

            // Store stream into a Bitmap
            BitmapImage src = new BitmapImage();
            await src.SetSourceAsync(stream);

            // Return a UIElelment
            Image image = new Image();
            image.Source = src;
            return image;
        }


    }
}
