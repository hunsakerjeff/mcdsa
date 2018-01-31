using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;

namespace DSA.Data.Services
{
    /// <summary>
    /// Sharing Service
    /// Share documents using email or DataTransferManager
    /// </summary>
    public class SharingService : ISharingService
    {
        #region Fields

        private DataTransferManager _transferManager;
        private List<StorageFile> _filesToShare;
        private List<string> _urlToShare;
        private readonly IFileService _fileService;
        private string _title;

        #endregion

        #region Constructor

        public SharingService(IFileService fileService)
        {
            _fileService = fileService;
        }

        #endregion

        #region MyRegion

        public async Task ShareMedia(MediaLink media)
        {
            if (SfdcConfig.EmailOnlyContentDistributionLinks)
            {
                await UseMailTo(media);
            }
            else
            {
                await UseDataTransfer(media);
            }
        }

        public async Task ShareMedia(List<MediaLink> mediaList, string toEmail)
        {
            if (SfdcConfig.EmailOnlyContentDistributionLinks)
            {
                await UseMailTo(mediaList, toEmail);
            }
            else
            {
                await UseDataTransfer(mediaList);
            }
        }

        private async Task UseMailTo(MediaLink media)
        {
            var mailtoBody = GetMailToLine(media, false);
            var mailto = new Uri($"mailto:?to=&subject={media.Name}&body={mailtoBody}");
            await Windows.System.Launcher.LaunchUriAsync(mailto);
        }

        private async Task UseMailTo(List<MediaLink> mediaList, string toEmail)
        {
            var mailBody = string.Join("%0d%0a", mediaList.Select(m => GetMailToLine(m, true)));
            var mailto = new Uri($"mailto:{toEmail}?body={mailBody}");
            await Windows.System.Launcher.LaunchUriAsync(mailto);
        }

        private object GetMailToLine(MediaLink media, bool includeName)
        {
            if (media.Type == MediaType.Url)
            {
                return media.Source;
            }

            return includeName
                    ? $"{media.Name}: {media.DistributionPublicUrl}"
                    : $"{media.DistributionPublicUrl}";
        }

        private async Task UseDataTransfer(MediaLink media)
        {
            ResetTranferData();
            _title = media.Name;
            if (media.Type == MediaType.Url)
            {
                _urlToShare = new List<string> { media.Source };
            }
            else
            {
                var file = await _fileService.GetMediaFile(media);
                _filesToShare = new List<StorageFile> { file };
            }
            DataTransferManager.ShowShareUI();
        }

        private async Task UseDataTransfer(List<MediaLink> mediaList)
        {
            ResetTranferData();
            _title = "Documents you requested";
            var tasks = mediaList.Where(m => m.Type != MediaType.Url).Select(async m => await _fileService.GetMediaFile(m)).ToList();
            var results = await Task.WhenAll(tasks);
            _filesToShare = results.ToList();
            _urlToShare = mediaList.Where(m => m.Type == MediaType.Url).Select(m => m.Source).ToList();
            DataTransferManager.ShowShareUI();
        }

        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataPackage requestData = args.Request.Data;
            requestData.Properties.Title = _title;
            if (_urlToShare != null && _urlToShare.Any())
            {
                requestData.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(string.Join("<br/>", _urlToShare)));
            }
            if (_filesToShare != null && _filesToShare.Any())
            {
                requestData.SetStorageItems(_filesToShare.ToArray());
            }
            ResetTranferData();
        }

        private void ResetTranferData()
        {
            _urlToShare = null;
            _filesToShare = null;
            _title = string.Empty;
        }

        public void Attach()
        {
            if (_transferManager != null)
            {
                _transferManager.DataRequested -= OnDataRequested;
            }
            _transferManager = DataTransferManager.GetForCurrentView();
            _transferManager.DataRequested += OnDataRequested;
        }

        #endregion
    }
}
