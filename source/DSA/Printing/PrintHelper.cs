using DSA.Model.Dto;
using DSA.Shell.Pages;
using Salesforce.SDK.Adaptation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation.Diagnostics;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Printing;


namespace DSA.Common
{
    public class PrintHelper
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        private IPrintDocumentSource _printDocumentSource;
        private MediaLink _media;
        private PdfDocument _pdfDocument;
        PrintPageDescription _pageDescription;

        // //////////////////////////////////////////////////////////
        // Constants
        // //////////////////////////////////////////////////////////
        private const int kWrongPassword = unchecked((int)0x8007052b); // HRESULT_FROM_WIN32(ERROR_WRONG_PASSWORD)
        private const int kGenericFail = unchecked((int)0x80004005);   // E_FAIL


        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public PrintDocument Document { get; set; }
        public bool DocumentLoaded { get; set; }
        internal List<PdfPageElement> Pages { get; set; }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public PrintHelper(MediaLink media)
        {
            // Set defaults
            DocumentLoaded = false;
            _pdfDocument = null;
            _media = media;

            // Allocate a pages list
            Pages = new List<PdfPageElement>();
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public virtual void RegisterForPrinting()
        {
            // Register for PrrintRequested event
            PrintManager printMgr = PrintManager.GetForCurrentView();
            printMgr.PrintTaskRequested += PrintTaskRequested;

            // Build a PrintDocument and register for callbacks
            Document = new PrintDocument();
            Document.Paginate += Paginate;
            Document.GetPreviewPage += GetPreviewPage;
            Document.AddPages += AddPages;
            _printDocumentSource = Document.DocumentSource;
        }

        public virtual void UnregisterForPrinting()
        {
            // Validate Object
            if (Document == null)
            {
                return;
            }

            // Unregister callbacks
            Document.Paginate -= Paginate;
            Document.GetPreviewPage -= GetPreviewPage;
            Document.AddPages -= AddPages;
            _printDocumentSource = null;

            // Remove the handler for printing initialization.
            PrintManager printMgr = PrintManager.GetForCurrentView();
            printMgr.PrintTaskRequested -= PrintTaskRequested;
        }

        public async Task ShowPrintUIAsync()
        {
            // Catch and print out any errors reported
            try
            {
                await PrintManager.ShowPrintUIAsync();
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger("PrintHelper.ShowPrintUIAsync - ShowPrintUIAsync threw an Exception", LoggingLevel.Error);
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }

        public void PreparePrintContent()
        {
            // Load the media item
            LoadDocumentAsync(_media);
        }



        // //////////////////////////////////////////////////////////
        // Implementation - Protected Methods
        // //////////////////////////////////////////////////////////
        protected virtual void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            // Create the print task
            PrintTask printTask = null;
            printTask = args.Request.CreatePrintTask("DSA Printing", PrintTaskSourceRequested);

            // Handle Print Task completed to catch filed jobs
            printTask.Completed += PrintTaskCompleted;

            // Configure standard print options
            //PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
            //IList<string> displayedOptions = printDetailedOptions.DisplayedOptions;

            // Choose the printer options to be shown.
            // The order in which the options are appended determines the order in which they appear in the UI
            //displayedOptions.Clear();

            //displayedOptions.Add(StandardPrintTaskOptions.Copies);
            //displayedOptions.Add(StandardPrintTaskOptions.Orientation);
            //displayedOptions.Add(StandardPrintTaskOptions.ColorMode);


            // Configure App Specific features for print dialog
            //ConfigurePrintTaskOptionDetails(printTask);
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Private Methods
        // //////////////////////////////////////////////////////////
        //private void ConfigurePrintTaskOptionDetails(PrintTask printTask)
        //{
        //    PrintTaskOptionDetails details = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);

        //    IList<String> displayOptions = details.DisplayedOptions;

        //    // Create a new list option
        //    PrintCustomItemListOptionDetails fit = details.CreateItemListOption("Fit", "Fit To Page");
        //    fit.AddItem("Scale", "Scale to Fit");
        //    fit.AddItem("Crop", "Crop to Fit");

        //    // Add the custom option to the option list, handle changes
        //    displayOptions.Add("Fit");
        //    details.OptionChanged += Details_OptionChanged;
        //}

        //private void Details_OptionChanged(PrintTaskOptionDetails sender, PrintTaskOptionChangedEventArgs args)
        //{
        //    // Find out what option changed
        //    if (args.OptionId != null && args.OptionId.ToString() == "Fit")
        //    {
        //        IPrintOptionDetails fit = sender.Options[args.OptionId.ToString()];

        //        //switch (fit.Value.ToString())
        //        //{
        //        //    // Hanlde selected options cases here
        //        //}
        //    }
        //}

        private void PrintTaskSourceRequested(PrintTaskSourceRequestedArgs args)
        {
            // Set the document Source
            args.SetSource(_printDocumentSource);
        }

        void Paginate(object sender, PaginateEventArgs args)
        {
            // Get the page description to deterimine how big the page is
            PrintTaskOptions printingOptions = ((PrintTaskOptions)args.PrintTaskOptions);
            _pageDescription = printingOptions.GetPageDescription(0);

            // Do work to determine how many pages are in the printable content
            Document.SetPreviewPageCount(this.Pages.Count, PreviewPageCountType.Final);
        }

        void GetPreviewPage(object sender, GetPreviewPageEventArgs args)
        {
            // Provide a UIElement as the print preview. (1 pased index for print pages) 
            int zeroIndex = args.PageNumber - 1;
            Document.SetPreviewPage(args.PageNumber, Pages[zeroIndex].Image as UIElement);
        }

        private void AddPages(object sender, AddPagesEventArgs e)
        {
            // Loop over all of the pages to print and add to PintDocument
            for (int i = 0; i < Pages.Count; i++)
            {
                var pageDesc = e.PrintTaskOptions.GetPageDescription((uint)i);
                var currentPage = Pages[i];

                // Render UIElement in target resolution; DIP = "1/96"
                Document.AddPage(currentPage.Image as UIElement);
                //Document.AddPage(await currentPage.GetPageInTargetResolution(pageDesc.ImageableRect.Width * DpiX / 96, pageDesc.ImageableRect.Height * DpiY / 96));
            }

            // Indicate that all of the print pages have been provided
            Document.AddPagesComplete();
        }

        private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                // Notify the user in UI
                MessageDialog dlg = new MessageDialog("Printing failed.");
                dlg.ShowAsync();
            }
        }


        // //////////////////////////////////////////////////////////
        // Implementation - PDF Processing
        // //////////////////////////////////////////////////////////
        private async Task LoadDocumentAsync(MediaLink media)
        {
            // Load based on media type
            switch (media.Type)
            {
                case MediaType.PDF:
                    var results = await LoadPdfDocument(media.Source);
                    DocumentLoaded = results;
                    break;

                default:
                    DocumentLoaded = false;
                    break;
            }
        }

        private async Task<bool> LoadPdfDocument(string filePath)
        {
            // Get the PDF Storagefile
            var pdfFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(filePath));

            // Load the PDF
            try
            {
                _pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile);
            }
            catch (Exception ex)
            {
                switch (ex.HResult)
                {
                    case kWrongPassword:    // ERROR:  Password protected PDF, log
                        PlatformAdapter.SendToCustomLogger("PrintHelper.LoadPdfDocument - Document is password-protected and password is incorrect", LoggingLevel.Error);
                        break;

                    case kGenericFail:      // ERROR:  Generic error, log
                        PlatformAdapter.SendToCustomLogger("PrintHelper.LoadPdfDocument - Document is not a valid PDF.", LoggingLevel.Error);
                        break;

                    default:                // ERROR:  All others, log
                        // File I/O errors are reported as exceptions.
                        PlatformAdapter.SendToCustomLogger( string.Format("PrintHelper.LoadPdfDocument - Generic Error loading PDF:  {0}.", ex.HResult), LoggingLevel.Error);
                        break;
                }
            }

            if (_pdfDocument != null)
            {
                if (_pdfDocument.IsPasswordProtected)
                {
                    // Log this
                    PlatformAdapter.SendToCustomLogger("PrintHelper.LoadPdfDocument - Document is password protected", LoggingLevel.Error);
                    return false;
                }

                // Render PDF pages into image elements
                for (uint index=0; index<_pdfDocument.PageCount; index++)
                {
                    // Add the the Pages List
                    PdfPageElement page = new PdfPageElement(_pdfDocument.GetPage(index));
                    await page.Initialize();
                    Pages.Add(page);
                }
                return true;
            }

            return false;
        }
    }
}
