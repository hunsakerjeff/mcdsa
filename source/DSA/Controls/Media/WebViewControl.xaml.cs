using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.Web;
using Salesforce.SDK.Adaptation;

namespace DSA.Shell.Controls.Media
{
    public sealed partial class WebViewControl
    {
        public WebViewControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty HtmlSourceProperty = DependencyProperty.RegisterAttached("HtmlSource", typeof(string), typeof(WebViewControl), new PropertyMetadata("", OnHtmlPropertyChanged));


        public static string GetHtmlSource(DependencyObject obj)
        {
            return (string)obj.GetValue(HtmlSourceProperty);
        }

        public static void SetHtmlSource(DependencyObject obj, string value)
        {
            obj.SetValue(HtmlSourceProperty, value);
        }

        private static async void OnHtmlPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var control = source as WebViewControl;
            var htmlSource = e.NewValue as String;

            try
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(htmlSource));
             
                StreamUriZipResolver myResolver = new StreamUriZipResolver(file);

                Uri url = control.webView.BuildLocalStreamUri("sfdcTag", "index.html");

                control.webView.NavigateToLocalStreamUri(url, myResolver);
            }
            catch
            {
                // TODO log exception
            }
        }

        public sealed class StreamUriZipResolver : IUriToStreamResolver
        {
            private readonly StorageFile _file;

            public StreamUriZipResolver(StorageFile file)
            {
                _file = file;
            }

            public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
            {
                if (uri == null)
                {
                    throw new Exception();
                }
                string path = uri.AbsolutePath;
                return GetContent(path).AsAsyncOperation();
            }

            private async Task<IInputStream> GetContent(string path)
            {
                if (path.ElementAt(0) == '/')
                {
                    path = path.Remove(0, 1);
                }

                Stream zipStream = await _file.OpenStreamForReadAsync();
                ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                ZipArchiveEntry contentFile = archive.GetEntry(path);

                if (contentFile == null)
                {
                    throw new Exception("Invalid archive entry");
                }
                
                try
                {
                    var zipASize = contentFile.Length;
                    IInputStream stream = await GetInputStreamFromIOStream(contentFile.Open(), zipASize);

                    return stream;
                }
                catch (Exception ex)
                {
                    PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                    return null;
                }
            }

            private async Task<IInputStream> GetInputStreamFromIOStream(Stream stream, long fileSize)
            {
                try
                {
                    using (BinaryReader binReader = new BinaryReader(stream))
                    {
                        byte[] byteArr = binReader.ReadBytes((int)fileSize);

                        using (var iMS = new InMemoryRandomAccessStream())
                        {
                            var imsOutputStream = iMS.GetOutputStreamAt(0);
                            using (DataWriter dataWriter = new DataWriter(imsOutputStream))
                            {
                                dataWriter.WriteBytes(byteArr);

                                await dataWriter.StoreAsync();
                                await imsOutputStream.FlushAsync();

                                return iMS.GetInputStreamAt(0);
                            }                                
                        }
                    }
                }
                catch (Exception ex)
                {
                    PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                    return null;
                }
            }
        }
    }
}
