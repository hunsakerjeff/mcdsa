using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Model.Messages;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects;
using GalaSoft.MvvmLight.Messaging;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Data.Services
{
    public class HistoryDataService : IHistoryDataService
    {
        private const string SoupName = "HistoryInfo";
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly IMobileAppConfigDataService _mobileAppConfigDataService;
        private readonly ISettingsDataService _settingsDataService;

        public HistoryDataService(
            IDocumentInfoDataService documentInfoDataService,
            ISettingsDataService settingsDataService,
            IMobileAppConfigDataService mobileAppConfigDataService)
        {
            _documentInfoDataService = documentInfoDataService;
            _settingsDataService = settingsDataService;
            _mobileAppConfigDataService = mobileAppConfigDataService;
        }

        public async Task<List<HistoryItemDto>> GetHistoryData()
        {
            var historyInfos = await GetAllHistoryInfo();
            var documentsIDs = historyInfos.Select(hi => hi.ContentDocumentID);
            var documents = await _documentInfoDataService.GetContentDocumentsByID(documentsIDs);
            var configurations = await _mobileAppConfigDataService.GetMobileAppConfigs();

            var historyInfosWithDocuments = historyInfos.Join(documents, hi => hi.ContentDocumentID, doc => doc.Document.Id, (hi, doc) => new { HistoryInfo = hi, Document = doc });
            var historyItems = historyInfosWithDocuments.Join(configurations, hdoc => hdoc.HistoryInfo.MobileConfigurationID, c => c.Id, (hdoc, c) => new HistoryItemDto() { Media = new MediaLink(hdoc.Document), ConfigurationName = c.Title, VisitedOn = hdoc.HistoryInfo.VisitedOn });

            return historyItems.OrderByDescending(hi => hi.VisitedOn).ToList();
        }

        public async Task AddToHistory(MediaLink media)
        {
            var currentMobileConfigurationID = await _settingsDataService.GetCurrentMobileConfigurationID();
            await Task.Factory.StartNew(() =>
            {
                var historyInfo = new HistoryInfoDto
                {
                    ContentDocumentID = media.Document.Id,
                    MobileConfigurationID = currentMobileConfigurationID,
                    VisitedOn = DateTime.Now
                };
                HistoryInfo.Instance.SaveToSoup(historyInfo);
                Messenger.Default.Send(new HistoryChangedMessage());
            });
        }

        private async Task<List<HistoryInfoDto>> GetAllHistoryInfo()
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                if (globalStore.HasSoup(SoupName) == false)
                {
                    return new List<HistoryInfoDto>();
                }

                var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<HistoryInfoDto>(item.ToString()))
                        .ToList();
            });
        }
    }
}
