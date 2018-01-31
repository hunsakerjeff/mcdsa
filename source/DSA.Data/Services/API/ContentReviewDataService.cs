using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.Sync;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;
using WinRTXamlToolkit.Tools;

namespace DSA.Data.Services.API
{
    public class ContentReviewDataService : IContentReviewDataService
    {
        //private const string SoupEntryId = "_soupEntryId";
        private const string SoupName = "ContentReview";
        private readonly IGeolocationService _geolocationService;

        public ContentReviewDataService(
            IGeolocationService geolocationService)
        {
            _geolocationService = geolocationService;
        }

        private IndexSpec[] _indexSpecs => new[]
        {
            new IndexSpec(SyncManager.LocallyCreated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyUpdated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyDeleted, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.Local, SmartStoreType.SmartString),
            new IndexSpec(Constants.Id, SmartStoreType.SmartString)
        };

        public async Task SaveCompleteReviewsToSoup(List<ContentReview> contentReviews)
        {
            await DispatcherHelper.RunAsync(async () =>
            {
                var location = await _geolocationService.GetLocation();
                var store = SmartStore.GetGlobalSmartStore();
                SetupSoupIfNotExistsNeeded(store, SoupName);
                contentReviews.ForEach(cr =>
                {
                    cr.GeolocationLatitude = location.Latitude;
                    cr.GeolocationLongitude = location.Longitude;
                });

                contentReviews.ForEach(cr => SaveReview(store, cr));
                SyncUpContentReview();
            });
        }

        public async Task SaveCompleteReviewToSoup(MediaLink mediaLink, bool emailed, double viewedTime, string playListId, string contactId)
        {
            await DispatcherHelper.RunAsync(async () =>
            {
                var location = await _geolocationService.GetLocation();
                var store = SmartStore.GetGlobalSmartStore();
                SetupSoupIfNotExistsNeeded(store, SoupName);

                var contentReview = new ContentReview
                {
                    ContentId = mediaLink.ID,
                    ContentTitle = mediaLink.Name,
                    PlaylistId = string.IsNullOrWhiteSpace(playListId) ? string.Empty : playListId,
                    ContactId = string.IsNullOrWhiteSpace(contactId) ? string.Empty : contactId,
                    GeolocationLatitude = location.Latitude,
                    GeolocationLongitude = location.Longitude,
                    DocumentEmailed = emailed,
                    Rating = 0,
                    TimeViewed = viewedTime
                };

                SaveReview(store, contentReview);
                SyncUpContentReview();
            });
        }

        private void SaveReview(SmartStore store, ContentReview contentReview)
        {
            var record = JObject.FromObject(contentReview,
                JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));

            record[SyncManager.Local] = true;

            var info = contentReview.GetType().GetTypeInfo().GetCustomAttributes()
                .SingleOrDefault(t => t is JsonObjectAttribute) as JsonObjectAttribute;
            if (info != null)
            {
                record[Constants.SobjectType] = string.Format(info.Title,
                    SfdcConfig.CustomerPrefix);
            }
            record[SyncManager.LocallyCreated] = true;
            record[SyncManager.LocallyUpdated] = false;
            record[SyncManager.LocallyUpdated] = false;
            record[Constants.Id] = Guid.NewGuid();
            store.Upsert(SoupName, record, Constants.Id, false);
        }

        public async void SyncUpContentReview()
        {
            try
            {
                await ObjectSyncDispatcher.Instance.SyncUpContentReview((text) => Debug.WriteLine(text));
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                Debug.WriteLine("SyncUpContentReview exception");
            }
        }

        private void SetupSoupIfNotExistsNeeded(ISmartStore store, string soupName)
        {
            if (!store.HasSoup(soupName))
            {
                store.RegisterSoup(soupName, _indexSpecs);
            }
        }
    }
}
