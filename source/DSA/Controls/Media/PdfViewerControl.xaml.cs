using System;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using DSA.Model.Messages;
using GalaSoft.MvvmLight.Messaging;
using PdfViewModel;
using Salesforce.SDK.Adaptation;
using WinRTXamlToolkit.IO.Extensions;

namespace DSA.Shell.Controls.Media
{
    public sealed partial class PdfViewerControl
    {
        private const double ProgressIndicationFileSizeLimitInMB = 1.9;

        public PdfViewerControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PdfSourceProperty = DependencyProperty.RegisterAttached("PdfSource", typeof(string), typeof(PdfViewerControl), new PropertyMetadata("", OnPdfPropertyChanged));

        public static string GetPdfSource(DependencyObject obj)
        {
            return (string)obj.GetValue(PdfSourceProperty);
        }

        public static void SetPdfSource(DependencyObject obj, string value)
        {
            obj.SetValue(PdfSourceProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register("IsLoading", typeof(bool), typeof(PdfViewerControl), new PropertyMetadata(false));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        public static readonly DependencyProperty IsPdfErrorProperty = DependencyProperty.Register("IsPdfError", typeof(bool), typeof(PdfViewerControl), new PropertyMetadata(false));

        public bool IsPdfError
        {
            get { return (bool)GetValue(IsPdfErrorProperty); }
            set { SetValue(IsPdfErrorProperty, value); }
        }

        private static async void OnPdfPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            PdfViewerControl control = source as PdfViewerControl;
            var pdfSource = e.NewValue as String;
            if (string.IsNullOrEmpty(pdfSource))
            {
                return;
            }

            try
            {
                if (control != null)
                {
                    control.ClearSource();
                    var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(pdfSource));
                    await ShowProgressIndicatorIfNecessary(control, file);
                    control.LoadPDF(file);
                }
            }
            catch
            {
                // TODO Log exception here
            }
        }

        private static async Task ShowProgressIndicatorIfNecessary(PdfViewerControl control, StorageFile file)
        {
            var size = await file.GetSizeAsync();
            var sizeInMB = size / 1048576.0;
            if (sizeInMB > ProgressIndicationFileSizeLimitInMB)
            {
                control.IsLoading = true;
                control.IsPdfError = false;
            }
        }

        private void ClearSource()
        {
            pdfListView.ItemsSource = null;
        }

        private async void LoadPDF(StorageFile pdfFile)
        {
            PdfDocument pdfDocument;
            try
            {
                pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile);

            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                IsLoading = false;
                IsPdfError = true;
                return;
            }
            if (pdfDocument != null)
            {
                Initialize(pdfDocument);
            }
        }

        private void Initialize(PdfDocument pdfDocument)
        {
            Size pageSize = new Size(Window.Current.Bounds.Width, Window.Current.Bounds.Height);

            var source = new PdfDocViewModel(pdfDocument, pageSize, SurfaceType.VirtualSurfaceImageSource);
            pdfListView.ItemsSource = source;
        }

        private Point _initialpoint;
        private void OnListViewItemManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
            _initialpoint = e.Position;
        }

        private void OnListViewItemManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            Point currentpoint = e.Position;

            var firstPosition = _initialpoint.X;
            var lastPosition = currentpoint.X;
            if (Math.Abs(firstPosition - lastPosition) > 200)
            {
                SwipeMessage message = firstPosition - lastPosition > 200
                                        ? SwipeMessage.Left as SwipeMessage
                                        : SwipeMessage.Right as SwipeMessage;

                Messenger.Default.Send(message);
            }
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            IsLoading = false;
            IsPdfError = false;
        }
    }
}
