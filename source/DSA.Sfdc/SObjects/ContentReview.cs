using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Salesforce.SDK.Auth;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Sfdc.SObjects
{
    internal class ContentReview : SObject
    {
        private readonly AsyncLock _mutex = new AsyncLock();

        private static readonly Lazy<ContentReview> Lazy = new Lazy<ContentReview>(() => new ContentReview(SmartStore.GetGlobalSmartStore()));

        public static ContentReview Instance => Lazy.Value;

        //private const string SpupEntryId = "_soupEntryId";

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec(Constants.Id, SmartStoreType.SmartString)
        };

        public Guid InstanceID { get; private set; }

        private ContentReview(SmartStore store) : base(store)
        {
            InstanceID = Guid.NewGuid();
            if (IndexedFieldsForSObjects != null)
                AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public Model.Models.ContentReview SaveReviewToSoup(Model.Dto.MediaLink mediaLink, bool emailed, double viewedTime, string playListId, string contactId)
        {
            SetupSoupIfNotExistsNeeded();

            var contentReview = new Model.Models.ContentReview
            {
                ContentId = mediaLink.ID,
                ContentTitle = mediaLink.Name,
                PlaylistId = string.IsNullOrWhiteSpace(playListId)? string.Empty : playListId,
                ContactId = string.IsNullOrWhiteSpace(contactId) ? string.Empty : contactId,
                DocumentEmailed = emailed,
                Rating = 0,
                TimeViewed = viewedTime
            };

            var record = JObject.FromObject(contentReview, JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings));

            record[SyncManager.Local] = true;

            var info = contentReview.GetType().GetTypeInfo().GetCustomAttributes()
                 .SingleOrDefault(t => t is JsonObjectAttribute) as JsonObjectAttribute;
            if (info != null)
            {
                record[Constants.SobjectType] = string.Format(info.Title, Prefix);
            }

            record[SyncManager.LocallyCreated] = true;
            record[SyncManager.LocallyUpdated] = false;
            record[SyncManager.LocallyUpdated] = false;
            record[Constants.Id] = Guid.NewGuid();

            Store?.Upsert(SoupName, record, Constants.Id, false);

            return contentReview;
        }

        private void SetupSoupIfNotExistsNeeded()
        {
            if (!Store.HasSoup(SoupName))
            {
                Store.RegisterSoup(SoupName, IndexSpecs);
            }
        }

        public async Task<SyncState> SyncUpContentReview(CancellationToken token = default(CancellationToken))
        {
            using (await _mutex.LockAsync(token))
            {
                SyncState syncResult = new SyncState { Status = SyncState.SyncStatusTypes.Failed };
                try
                {
                    var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, Constants.Id, QuerySpec.SqlOrder.ASC, PageSize);
                    var reviews = Store.Query(querySpec, 0).Select(x => x.ToObject<JObject>()).ToList();
                    var properties = JObject.FromObject(new Model.Models.ContentReview(), JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings))
                        .Properties().Select(p => p.Name).ToList();
                    var options = SyncOptions.OptionsForSyncUp(properties, SyncState.MergeModeOptions.None);
                    if (!reviews.Any())
                        return new SyncState { Status = SyncState.SyncStatusTypes.Done };
                    var review = reviews.FirstOrDefault();
                    var target = new SyncUpTarget(review, true);
                    var syncManager = SyncManager.GetInstance(AccountManager.GetAccount());
                    return await syncManager.SyncUp(target, options, SoupName, null, token);
                }
                catch (SmartStoreException sse)
                {
                    CreateLogItem("SmartStoreException", sse);
                }
                catch (OperationCanceledException oce)
                {
                    CreateLogItem("OperationCanceledException", oce);
                    throw;
                }
                catch (Exception e)
                {
                    CreateLogItem("General exception", e);
                }
                return syncResult;
            }
        }

        public IList<Model.Models.ContentReview> GetAll()
        {
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, Constants.Id, QuerySpec.SqlOrder.ASC, PageSize);
            var results =
                Store?.Query(querySpec, 0)
                    .Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.ContentReview>(item.ToString()))
                    .ToList();
            return results;
        }
    }
}
