using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Data.Services.API
{
    public class EventDataService : IEventDataService
    {
        private const string SoupName = "Event";
        private const string EventSubject = "DSA Presentation";

        private const string EventDescriptionFormat =
            "Presented the following documents using DSA application: {0}; \nOther Notes: {1}";
        
        private IndexSpec[] _indexSpecs => new[]
        {
            new IndexSpec(SyncManager.LocallyCreated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyUpdated, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.LocallyDeleted, SmartStoreType.SmartString),
            new IndexSpec(SyncManager.Local, SmartStoreType.SmartString),
            new IndexSpec(Constants.Id, SmartStoreType.SmartString)
        };

        public async Task SaveEventToSoup(string notes, IList<ContentReviewInfo> contentReviews)
        {
            await Task.Factory.StartNew(() =>
            {
                var store = SmartStore.GetGlobalSmartStore();
                SetupSoupIfNotExistsNeeded(store, SoupName);

                if(!contentReviews.Any())
                    return;

                var documentTitles = contentReviews.Select((review, index) => $"{index + 1}. {review.ContentReview.ContentTitle}").OrderBy(x => x).ToList();

                var durationInSeconds = contentReviews.Sum(review => review.ContentReview.TimeViewed);
                var durationInMinutes = (int)(durationInSeconds/60);
                
                var eventRecord = new Event
                {
                   Subject = EventSubject,
                   ActivityDateTime = DateTime.UtcNow,
                   DurationInMinutes = durationInMinutes,
                   Description = string.Format(EventDescriptionFormat, string.Join(",", documentTitles), notes),
                   WhoId = contentReviews.FirstOrDefault()?.ContentReview.ContactId
                };

                var record = JObject.FromObject(eventRecord, JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));
                record[Constants.SobjectType] = SoupName;
                record[SyncManager.Local] = true;
                record[SyncManager.LocallyCreated] = true;
                record[SyncManager.LocallyUpdated] = false;
                record[SyncManager.LocallyUpdated] = false;
                record[Constants.Id] = Guid.NewGuid();
                store.Upsert(SoupName, record, Constants.Id, false);
            });
        }

        public async void SyncUpEvents()
        {
            try
            {
                if (!ObjectSyncDispatcher.HasInternetConnection())
                    return;

                await ObjectSyncDispatcher.Instance.SyncUpEvents((text) => Debug.WriteLine(text));
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                Debug.WriteLine("SyncUpEvents Exception");
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
