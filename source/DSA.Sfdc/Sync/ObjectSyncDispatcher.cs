using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Networking.Connectivity;
using Windows.Storage;
using DSA.Common.Utils;
using DSA.Model;
using DSA.Model.Messages;
using DSA.Model.Models;
using DSA.Sfdc.DataWriters;
using DSA.Sfdc.SObjects;
using DSA.Sfdc.Storage;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Nito.AsyncEx;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Auth;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.Utilities;
using Category = DSA.Sfdc.SObjects.Category;
using CategoryContent = DSA.Sfdc.SObjects.CategoryContent;
using CategoryMobileConfig = DSA.Sfdc.SObjects.CategoryMobileConfig;
using ContentDocument = DSA.Sfdc.SObjects.ContentDocument;
using ContentReview = DSA.Sfdc.SObjects.ContentReview;
using EntitySubscription = DSA.Sfdc.SObjects.EntitySubscription;
using Event = DSA.Sfdc.SObjects.Event;
using MobileAppConfig = DSA.Sfdc.SObjects.MobileAppConfig;
using NewCategoryContent = DSA.Sfdc.SObjects.NewCategoryContent;
using Playlist = DSA.Sfdc.SObjects.Playlist;
using PlaylistContent = DSA.Sfdc.SObjects.PlaylistContent;


namespace DSA.Sfdc.Sync
{
    public class ObjectSyncDispatcher
    {
        #region Constants
        
        private const int kIdListLimit = 300;

        #endregion

        #region Fields and Properties

        private readonly AsyncLock _mutex = new AsyncLock();

        private static readonly Lazy<ObjectSyncDispatcher> Lazy = new Lazy<ObjectSyncDispatcher>(() => new ObjectSyncDispatcher());

        public static ObjectSyncDispatcher Instance => Lazy.Value;

        private readonly SmartStore _store;

        private static readonly int _concurrentTasksLimit = SfdcConfig.ConcurrentDownloadsLimit;

        private IList<SuccessfulSync> _backupSyncStates;

        private List<Func<Task<bool>>> _funcsToRetry;

        public static readonly string NoInternetConnectionMessage = "No Internet Connection";

        #endregion

        #region Constructor

        private ObjectSyncDispatcher()
        {
            _store = SmartStore.GetGlobalSmartStore();
            AttachMessages();
        }

        #endregion

        #region Methods

        #region Full Sync

        public async Task<User> QueueingFullSyncAsync(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            HasInternetConnectionOrThrow();
            await CheckAvailableSpace(0);
            _backupSyncStates = SuccessfulSyncState.Instance.GetAllSuccessfulSyncsFromSoup();
            SuccessfulSyncState.Instance.RecreateClearSoup();
            return await SyncAndGetCurrentUserFromSfdc(callbackHandler, token);
        }

        public async Task ConfigurationFullSyncAsync(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            var syncUpTasks = new List<Task>
            {
                SyncUpDsaSyncLogs(callbackHandler, token),
                SyncUpContentReview(callbackHandler,token),
                SyncUpEvents(callbackHandler,token),
                SyncUpPlaylistsAndContent(callbackHandler,token),
                SyncUpSearchTerms(callbackHandler, token)
            };
            await Task.WhenAll(syncUpTasks);

            // We have dependencies between the SyncDowns so they need to execute in a specifc order
            var syncDownTasks = new List<Task>
            {
                SyncFeaturedPlaylists(callbackHandler, currentUser, token),
                SyncContacts(callbackHandler, currentUser, token),
                SyncMobileAppConfigs(callbackHandler, currentUser, token),
                SyncCategoryMobileConfigs(callbackHandler, currentUser, token)
            };

            await Task.WhenAll(syncDownTasks);

            // Execute these in order
            (SyncCategories(callbackHandler, currentUser, token)).Wait();
            (SyncCategoryContent(callbackHandler, currentUser, false, token)).Wait();
        }

        public async Task ContentFullSyncAsync(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            //Stopwatch swMeta = new Stopwatch();
            //swMeta.Start();

            var mobileAppConfgsAttMeta = await SyncAndGetMobileAppConfigsAttMeta(callbackHandler, currentUser, token);
            var categoryMobileConfigAttMeta = await SyncAndGetCategoryMobileConfigAttMeta(callbackHandler, currentUser, token);
            var categoryAttMeta = await SyncAndGetCategoryAttMeta(callbackHandler, currentUser, token);
            var syncResult = await SyncAndGetMetadataOfContentDocumentsInLibraries(callbackHandler, token);
            var contentThumbnailAttMeta = await SyncAndGetContentThumbnailAttMeta(callbackHandler, token);
            //swMeta.Stop();

            var sizeBytesToDownload = 0m;
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(mobileAppConfgsAttMeta);
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(categoryMobileConfigAttMeta);
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(categoryAttMeta, new AttMetaDropSameFilesForDeltaSieve(_store));
            sizeBytesToDownload += GetSizeBytesToDownloadContentDocumentHelper(syncResult, new DocMetaByOwnershipAndUsageInCategoriesSieve(_store, currentUser));
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(contentThumbnailAttMeta);

            await CheckAvailableSpace(sizeBytesToDownload);
            Messenger.Default.Send(new SynchronizationProgressMessage(0m, sizeBytesToDownload));

            //Stopwatch swAtt = new Stopwatch();
            //swAtt.Start();
            List<Func<Task<bool>>> funcs = new List<Func<Task<bool>>>();
            _funcsToRetry = new List<Func<Task<bool>>>();
            funcs.AddRange(SyncAttachments(callbackHandler, mobileAppConfgsAttMeta, token));
            funcs.AddRange(SyncAttachments(callbackHandler, categoryMobileConfigAttMeta, token));
            funcs.AddRange(SyncAttachmentsDelta(callbackHandler, categoryAttMeta, token));
            funcs.AddRange(SyncCurrentVersionOfContentDocumentsInLibraries(callbackHandler, syncResult, currentUser, token));
            funcs.AddRange(SyncContentThumbnails(callbackHandler, contentThumbnailAttMeta, token));

            await RunAsyncQueueInBatches(funcs);
            await RunRetryFuncsIfAny();
            //swAtt.Stop();

            //Stopwatch swIndex = new Stopwatch();
            //swIndex.Start();
            await SyncIndexesForContentDocuments(callbackHandler, syncResult, token);
            //swIndex.Stop();

            //Stopwatch swCD = new Stopwatch();
            //swCD.Start();
            await SyncMetadataOfContentDistribution(callbackHandler, syncResult, true, token);
            //swCD.Stop();


        }

        #endregion


        #region Delta Sync

        public async Task<User> QueueingDeltaSyncAsync(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            HasInternetConnectionOrThrow();
            await CheckAvailableSpace(0);
            return await SyncAndGetCurrentUserFromSfdc(callbackHandler, token);
        }

        public async Task ConfigurationDeltaSyncAsync(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            var syncUpTasks = new List<Task>
            {
                SyncUpDsaSyncLogs(callbackHandler, token),
                SyncUpContentReview(callbackHandler,token),
                SyncUpEvents(callbackHandler,token),
                SyncUpPlaylistsAndContent(callbackHandler,token),
                SyncUpSearchTerms(callbackHandler, token)
            };
            await Task.WhenAll(syncUpTasks);

            // We have dependencies between the SyncDowns so they need to execute in a specifc order
            var syncDownTasks = new List<Task>
            {
                SyncFeaturedPlaylists(callbackHandler, currentUser, token),
                SyncContacts(callbackHandler, currentUser, token),
                SyncMobileAppConfigs(callbackHandler, currentUser, token),
                SyncCategoryMobileConfigs(callbackHandler, currentUser, token)
            };

            await Task.WhenAll(syncDownTasks);

            // Execute these in order
            (SyncCategories(callbackHandler, currentUser, token)).Wait();
            (SyncCategoryContent(callbackHandler, currentUser, true, token)).Wait();
        }

        public async Task ContentDeltaSyncAsync(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            var mobileAppConfigsAttMeta = await SyncAndGetMobileAppConfigsAttMeta(callbackHandler, currentUser, token);
            var categoryMobileConfigAttMeta = await SyncAndGetCategoryMobileConfigAttMeta(callbackHandler, currentUser, token);
            var categoryAttMeta = await SyncAndGetCategoryAttMeta(callbackHandler, currentUser, token);
            var syncResult = await SyncAndGetMetadataOfContentDocumentsInLibraries(callbackHandler, token);
            var contentThumbnailAttMeta = await SyncAndGetContentThumbnailAttMeta(callbackHandler, token);

            var sizeBytesToDownload = 0m;
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(mobileAppConfigsAttMeta, new AttMetaDropSameFilesForDeltaSieve(_store));
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(categoryMobileConfigAttMeta, new AttMetaDropSameFilesForDeltaSieve(_store));
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(categoryAttMeta, new AttMetaDropSameFilesForDeltaSieve(_store));
            sizeBytesToDownload += GetSizeBytesToDownloadContentDocumentHelper(syncResult, new DocMetaDeltaSieve(_store, currentUser));
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(contentThumbnailAttMeta, new AttMetaDropUnchangedFilesForDeltaSieve(_store));

            await CheckAvailableSpace(sizeBytesToDownload);
            Messenger.Default.Send(new SynchronizationProgressMessage(0m, sizeBytesToDownload));

            List<Func<Task<bool>>> funcs = new List<Func<Task<bool>>>();
            _funcsToRetry = new List<Func<Task<bool>>>();
            funcs.AddRange(SyncAttachmentsDelta(callbackHandler, mobileAppConfigsAttMeta, token));
            funcs.AddRange(SyncAttachmentsDelta(callbackHandler, categoryMobileConfigAttMeta, token));
            funcs.AddRange(SyncAttachmentsDelta(callbackHandler, categoryAttMeta, token));
            funcs.AddRange(SyncCurrentVersionOfContentDocumentsInLibrariesDelta(callbackHandler, syncResult, currentUser, token));
            funcs.AddRange(SyncContentThumbnailsDelta(callbackHandler, contentThumbnailAttMeta, token));

            await RunAsyncQueueInBatches(funcs);
            await RunRetryFuncsIfAny();

            await SyncIndexesForContentDocumentsDelta(callbackHandler, syncResult, token);
            await SyncMetadataOfContentDistribution(callbackHandler, syncResult, false, token);
        }

        #endregion


        #region Sync Up Functions
        public async Task SyncUpPlaylistsAndContent(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            await SyncUpFeaturedPlaylists(callbackHandler, token);
            await SyncUpPlaylistContent(callbackHandler, token);
        }

        public async Task SyncUpFeaturedPlaylists(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Featured Playlists", false, true);
            callbackHandler("Sync up Featured Playlists");
            var configsState = await Playlist.SyncUpInstance.SyncUpRecords(callbackHandler, token, Playlist.SyncUpInstance.LocalIdChangeHandler);

            await CheckAndHandleTokenExpiredSyncState(configsState);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlists not synced");
            }
            SendSyncDetailMessage("Featured Playlists", true, true);
        }

        public async Task SyncUpPlaylistContent(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Playlist Content", false, true);
            callbackHandler("Sync up Playlist Content");
            var configsState = await PlaylistContent.SyncUpInstance.SyncUpRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlist Content not synced");
            }
            SendSyncDetailMessage("Playlist Content", true, true);
        }

        public async Task SyncUpContentReview(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Content Reviews", false, true);
            await Task.Factory.StartNew(async () =>
            {
                if (!HasInternetConnection())
                    return;

                callbackHandler("Sync up for content Reviews");
                var configsState = await ContentReview.Instance.SyncUpContentReview(token);
                await CheckAndHandleTokenExpiredSyncState(configsState);
            }, token);
            SendSyncDetailMessage("Content Reviews", true, true);
        }

        public async Task SyncUpEvents(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Events", false, true);
            callbackHandler("Sync up Events");
            var configsState = await Event.Instance.SyncUpRecords(callbackHandler, token);

            await CheckAndHandleTokenExpiredSyncState(configsState);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Events not synced");
            }
            SendSyncDetailMessage("Events", true, true);
        }

        public async Task SyncUpDsaSyncLogs(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("DSA SyncLogs", false, true);
            callbackHandler("Sync up DSA SyncLogs");
            var configsState = await DsaSyncLog.Instance.SyncUpRecords(callbackHandler, token);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                //throw new SyncException("DSA SyncLogs not synced");
                callbackHandler("DSA SyncLogs not synced");
            }
            SendSyncDetailMessage("DSA SyncLogs", true, true);
        }

        public async Task SyncUpSearchTerms(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!SfdcConfig.CollectSearchTerms)
            {
                return;
            }

            SendSyncDetailMessage("Search Terms", false, true);
            callbackHandler("Sync up SearchTerms");
            var searchTerm = new SearchTerm(_store);
            var configsState = await searchTerm.SyncUpRecords(callbackHandler, token);

            await CheckAndHandleTokenExpiredSyncState(configsState);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                callbackHandler("SearchTerms not synced");
                //throw new SyncException("SearchTerms not synced");
            }
            SendSyncDetailMessage("Search Terms", true, true);
        }

        #endregion


        #region Sync Functions

        private async Task SyncMobileAppConfigs(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Mobile App Config", false, false);
            callbackHandler("Sync Mobile App Config");
            var configs = new MobileAppConfig(_store, currentUser);

            // Perform Sync
            var configsState = await configs.SyncDownRecords(callbackHandler, token);

            // Check Results
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("MobileAppConfigs not synced");
            }
            SendSyncDetailMessage("Mobile App Config", true, false);
        }

        private async Task SyncCategoryMobileConfigs(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Category Mobile Configs", false, false);
            callbackHandler("Sync Category Mobile Configs");
            var configs = new CategoryMobileConfig(_store, currentUser);

            // Perform Sync
            var configsState = await configs.SyncDownRecords(callbackHandler, token);

            // Check Results
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("MobileAppConfigs not synced");
            }
            SendSyncDetailMessage("Category Mobile Configs", true, false);
        }

        private async Task SyncCategories(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Categories", false, false);

            // Setup variables
            var cmc = new CategoryMobileConfig(_store, currentUser);
            var categoryIdList = cmc.GetCategoryIds();

            // Set up a loop to sync in blocks
            callbackHandler("Sync Categories");
            var category = new Category(_store, currentUser);
            int index = 0;
            int rem = categoryIdList.Count;

            while (rem > 0)
            {
                int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                // Get range of Ids to sync
                category.CategoryIdList = categoryIdList.GetRange(index, idCount);

                // Perform Sync
                var configsState = (index > 0) ? await category.SyncDownRecordsToSoup(callbackHandler, token) : await category.SyncDownRecords(callbackHandler, token);

                // Check Results
                if (configsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("Categories not synced");
                }

                // Iterate to next sublist
                index += idCount;
                rem = categoryIdList.Count - index;
            }
            SendSyncDetailMessage("Categories", true, false);
        }

        private async Task SyncCategoryContent(Action<string> callbackHandler, User currentUser, bool saveNew, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Category Content", false, false);

            // Setup variables
            var cat = new Category(_store, currentUser);
            List<string> categoryIdList = cat.GetIds();

            callbackHandler("Sync Category Content");
            var categoryContent = new CategoryContent(_store);
            var categoryContentBeforeSync = categoryContent.GetFromSoup().ToList();
            int index = 0;
            int rem = categoryIdList.Count;

            while (rem > 0)
            {
                int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                // Get range of Ids to sync
                categoryContent.CategoryIdList = categoryIdList.GetRange(index, idCount);

                // Perform Sync
                var configsState = (index > 0) ? await categoryContent.SyncDownRecordsToSoup(callbackHandler, token) : await categoryContent.SyncDownRecords(callbackHandler, token);

                // Check Results
                if (configsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("Category Content not synced");
                }

                // Iterate to next sublist
                index += idCount;
                rem = categoryIdList.Count - index;
            }

            NewCategoryContent.Instance.RecreateClearSoup();
            if (saveNew)
            {
                var categoryContentAfterSync = categoryContent.GetFromSoup().ToList();
                var newCategoryContents = categoryContentAfterSync.Except(categoryContentAfterSync.Join(categoryContentBeforeSync, cc1 => cc1.Id, cc2 => cc2.Id, (cc1, cc2) => cc1)).ToList();
                NewCategoryContent.Instance.SaveToSoup(newCategoryContents);
            }

            SendSyncDetailMessage("Category Content", true, false);
        }

        private async Task SyncContacts(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Contacts", false, false);
            callbackHandler("Sync Contacts");
            var category = new Contact(_store, currentUser);

            // Perform Sync
            var configsState = await category.SyncDownRecords(callbackHandler, token);

            // Check Results
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Contacts not synced");
            }
            SendSyncDetailMessage("Contacts", true, false);
        }

        private async Task SyncFeaturedPlaylists(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            // Get playlist which I own, are shareable internally or are marked as featured 
            SendSyncDetailMessage("Featured Playlists", false, false);
            callbackHandler("Sync Featured Playlists");
            var playlist = new Playlist(_store, currentUser);
            var configsState = await playlist.SyncDownRecords(callbackHandler, token);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlists not synced");
            }
            SendSyncDetailMessage("Featured Playlists", true, false);

            //get entry subscription it is required to retrieve playlist which are follwed by the user
            SendSyncDetailMessage("User Subscription", false, false);
            callbackHandler("Sync User Subscription");
            var entity = new EntitySubscription(_store, currentUser);
            configsState = await entity.SyncDownRecords(callbackHandler, token);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("User Subscription not synced");
            }
            SendSyncDetailMessage("User Subscription", true, false);

            //get playlist which I follow
            SendSyncDetailMessage("Followed Playlists", false, false);
            callbackHandler("Sync Followed Playlists");
            playlist.GetPlaylistWhichIFollow = true;
            playlist.DontClearSoup();
            configsState = await playlist.SyncDownRecords(callbackHandler, token);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlists not synced");
            }
            SendSyncDetailMessage("Followed Playlists", true, false);

            //sync playlist Content
            SendSyncDetailMessage("Playlist Content", false, false);
            callbackHandler("Sync Playlist Content");
            var playlistContent = new PlaylistContent(_store, currentUser);
            configsState = await playlistContent.SyncDownRecords(callbackHandler, token);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlist Content not synced");
            }
            SendSyncDetailMessage("Playlist Content", true, false);
        }

        #endregion


        #region Sync Attachment/Content Full Functions
        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetMobileAppConfigsAttMeta(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Mobile App Configs Metadata", false, false);

            // Setup variables
            var mac = new MobileAppConfig(_store, currentUser);
            var macList = mac.GetFromSoup().ToList();    // already unique
            List<string> macIdList = new List<string>();

            // Parse the Mac List and grab the Ids
            foreach (var macModel in macList)
            {
                macIdList.Add(macModel.Id);
            }

            // Set up a loop to sync in blocks
            callbackHandler("Sync Metadata Of Mobile App Config");
            var macAtt = new MobileAppConfigAttachment(_store);
            int index = 0;
            int rem = macIdList.Count;

            SyncState configsState = new SyncState();
            while (rem > 0)
            {
                int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                // Get range of Ids to sync
                macAtt.MacIdList = macIdList.GetRange(index, idCount);

                // Perform Sync
                var localConfigsState = await macAtt.SyncDownRecordsToSoup(callbackHandler, token);

                // Check Results
                if (localConfigsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("MobileAppConfig Attachment Metadata not synced");
                }

                // Update SyncState
                if (index == 0)
                {
                    configsState = localConfigsState;
                }
                else
                {
                    configsState.TotalSize += localConfigsState.TotalSize;
                }

                // Iterate to next sublist
                index += idCount;
                rem = macIdList.Count - index;
            }

            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.MobileAppConfigs);

            SendSyncDetailMessage("Mobile App Configs Metadata", true, false);

            return new SyncResult<IList<AttachmentMetadata>>(configsState, macAtt.GetMobileAppConfigAttachmentsFromSoup());
        }

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetCategoryMobileConfigAttMeta(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Category Mobile Config Metadata", false, false);

            // Setup variables
            var cmc = new CategoryMobileConfig(_store, currentUser);
            var cmcList = cmc.GetFromSoup().ToList();    // already unique
            List<string> cmcIdList = new List<string>();

            // Parse the Mac List and grab the Ids
            foreach (var cmcModel in cmcList)
            {
                cmcIdList.Add(cmcModel.Id);
            }

            // Set up a loop to sync in blocks
            callbackHandler("Sync Categories Of Mobile App Config");
            var cmcAtt = new CategoryMobileConfigAttachment(_store);
            int index = 0;
            int rem = cmcIdList.Count;

            SyncState configsState = new SyncState();
            while (rem > 0)
            {
                int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                // Get range of Ids to sync
                cmcAtt.CmcIdList = cmcIdList.GetRange(index, idCount);

                // Perform Sync
                var localConfigsState = await cmcAtt.SyncDownRecordsToSoup(callbackHandler, token);

                // Check Results
                if (localConfigsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("CategoryMobileConfig Attachment Metadata not synced");
                }

                // Update SyncState
                if (index == 0)
                {
                    configsState = localConfigsState;
                }
                else
                {
                    configsState.TotalSize += localConfigsState.TotalSize;
                }

                // Iterate to next sublist
                index += idCount;
                rem = cmcIdList.Count - index;
            }

            // Saving metadata successful sync
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.CategoryMobileConfigs);

            SendSyncDetailMessage("Category Mobile Config Metadata", true, false);

            // Create task List
            return new SyncResult<IList<AttachmentMetadata>>(configsState, cmcAtt.GetCategoryMobileAttachmentsFromSoup());
        }

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetCategoryAttMeta(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Category Metadata", false, false);

            // Setup variables
            var category = new Category(_store, currentUser);
            var categoryList = category.GetFromSoup().ToList();    // already unique
            List<string> categoryIdList = new List<string>();

            // Parse the Mac List and grab the Ids
            foreach (var categoryModel in categoryList)
            {
                categoryIdList.Add(categoryModel.Id);
            }

            // Set up a loop to sync in blocks
            callbackHandler("Sync Metadata Of Category attachments");
            var categoryAtt = new CategoryAttachment(_store);
            int index = 0;
            int rem = categoryIdList.Count;

            SyncState configsState = new SyncState();
            while (rem > 0)
            {
                int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                // Get range of Ids to sync
                categoryAtt.CategoryIdList = categoryIdList.GetRange(index, idCount);

                // Perform Sync
                var localConfigsState = await categoryAtt.SyncDownRecordsToSoup(callbackHandler, token);

                // Check Results
                if (localConfigsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("Category Attachment Metadata not synced");
                }

                // Update SyncState
                if (index == 0)
                {
                    configsState = localConfigsState;
                }
                else
                {
                    configsState.TotalSize += localConfigsState.TotalSize;
                }

                // Iterate to next sublist
                index += idCount;
                rem = categoryIdList.Count - index;
            }

            // Saving metadata successful sync
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.MobileAppConfigs);

            SendSyncDetailMessage("Category Metadata", true, false);

            // Create task List
            return new SyncResult<IList<AttachmentMetadata>>(configsState, categoryAtt.GetCategoryAttachmentsFromSoup());
        }

        private async Task<SyncResult<IList<Model.Models.ContentDocument>>> SyncAndGetMetadataOfContentDocumentsInLibraries(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Metadata of Content Documents", false, false);

            // Setup variables
            var categoryContent = new CategoryContent(_store);
            var contentIdList = categoryContent.GetContentIds();

            // Set up a loop to sync in blocks
            callbackHandler("Sync Metadata Of Content Documents");
            var contentDocument = new ContentDocument(_store);
            int index = 0;
            int rem = contentIdList.Count;

            SyncState configsState = new SyncState();
            while (rem > 0)
            {
                int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                // Get range of Ids to sync
                contentDocument.ContentIdList = contentIdList.GetRange(index, idCount);

                // Perform Sync
                var localConfigsState = (index > 0) ? await contentDocument.SyncDownRecordsToSoup(callbackHandler, token) : await contentDocument.SyncDownRecords(callbackHandler, token);

                // Check Results
                if (localConfigsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("Metadata for content documents not synced");
                }

                // Update SyncState
                if (index == 0)
                {
                    configsState = localConfigsState;
                }
                else
                {
                    configsState.TotalSize += localConfigsState.TotalSize;
                }

                // Iterate to next sublist
                index += idCount;
                rem = contentIdList.Count - index;
            }

            // saving metadata successful sync
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.ContentDocuments);

            SendSyncDetailMessage("Metadata of Content Documents", true, false);

            // Create task List
            return new SyncResult<IList<Model.Models.ContentDocument>>(configsState, contentDocument.GetFromSoup());
        }

        private List<Func<Task<bool>>> SyncAttachments(Action<string> callbackHandler, SyncResult<IList<AttachmentMetadata>> attMetaTransactionResult, CancellationToken token = default(CancellationToken))
        {
            var metaSyncState = attMetaTransactionResult.SyncState;
            var funcs = new List<Func<Task<bool>>>();

            foreach (var attMeta in attMetaTransactionResult.Result)
            {
                funcs.Add(async () => await SyncAttachmentsHelper(attMeta, metaSyncState, callbackHandler, token));
            }

            return funcs;
        }

        private List<Func<Task<bool>>> SyncCurrentVersionOfContentDocumentsInLibraries(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, User currentUser, CancellationToken token = default(CancellationToken))
        {
            var sieve = new DocMetaByOwnershipAndUsageInCategoriesSieve(_store, currentUser);
            var filesUsedInCategoryOrPrivateFilesMeta = contentDocuments.ResultFiltered(sieve);
            var contentSize = (long)filesUsedInCategoryOrPrivateFilesMeta.Sum(x => x.ContentSize);

            DsaSyncLog.Instance.SetDownloadedFilesInfo(filesUsedInCategoryOrPrivateFilesMeta.Count, contentSize);

            var funcs = new List<Func<Task<bool>>>();

            foreach (var docMeta in filesUsedInCategoryOrPrivateFilesMeta)
            {
                funcs.Add(async () => await SyncCurrentVersionOfContentDocumentsInLibrariesHelper(callbackHandler, docMeta, contentDocuments.SyncState, currentUser, token));
            }

            return funcs;
        }

        private async Task SyncIndexesForContentDocuments(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Indexes for Content Documents", false, false);

            callbackHandler("Sync Indexes for content Documents");
            var saveIndexesTasks = contentDocuments.Result.AsEnumerable().Select(doc => SaveSearchIndexesToSoup(doc, token));
            await Task.WhenAll(saveIndexesTasks);

            SendSyncDetailMessage("Indexes for Content Documents", true, false);
        }

        private async Task SyncMetadataOfContentDistribution(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, bool fullSync, CancellationToken token = default(CancellationToken))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (SfdcConfig.EmailOnlyContentDistributionLinks)
            {
                SendSyncDetailMessage("Metadata Of Content Distribution", false, false);
                callbackHandler("Sync Metadata Of Content Distribution");
                var meta = new ContentDistribution(_store, fullSync);
                var configsState = await meta.SyncDownRecords(callbackHandler, token);
                if (configsState.Status != SyncState.SyncStatusTypes.Done)
                {
                    throw new SyncException("Metadata for content Distribution not synced");
                }

                SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.ContentDistribution);

                await meta.ClearNotNeededContentDistribution(contentDocuments.Result);

                SendSyncDetailMessage("Metadata Of Content Distribution", true, false);
            }
        }

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetContentThumbnailAttMeta(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Metadata Of Content Thumbnail", false, false);
            callbackHandler("Sync Metadata Of Content Thumbnail attachments");
            var contentThumbnailsAtt = new ContentThumbnailAttachment(_store);

            // Sync the metadata
            var contentThumbnailsState = await contentThumbnailsAtt.SyncDownRecordsToSoup(callbackHandler, token);

            // Check the results
            if (contentThumbnailsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("ContentThumbnailAttMeta metadata not synced");
            }

            // FIXME: should Content Thumbnails have their own meta type?
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(contentThumbnailsState, SuccessfulSyncState.SyncedObjectMetaType.ContentThumbnails);

            SendSyncDetailMessage("Metadata Of Content Thumbnail", true, false);

            return new SyncResult<IList<AttachmentMetadata>>(contentThumbnailsState, contentThumbnailsAtt.GetContentThumbnailAttachmentsFromSoup());
        }

        private List<Func<Task<bool>>> SyncContentThumbnails(Action<string> callbackHandler, SyncResult<IList<AttachmentMetadata>> attMetaTransactionResult, CancellationToken token = default(CancellationToken))
        {
            var metaSyncState = attMetaTransactionResult.SyncState;
            var funcs = new List<Func<Task<bool>>>();

            foreach (var attMeta in attMetaTransactionResult.Result)
            {
                funcs.Add(async () => await SyncContentThumbnailsHelper(attMeta, metaSyncState, callbackHandler, metaSyncState, token));
            }
            return funcs;
        }
        #endregion


        #region Sync Attachment/Content Delta Functions

        private List<Func<Task<bool>>> SyncAttachmentsDelta(Action<string> callbackHandler, SyncResult<IList<AttachmentMetadata>> attMetaTransactionResult, CancellationToken token = default(CancellationToken))
        {
            var metaSyncState = attMetaTransactionResult.SyncState;

            // add sieve here
            var deltaSieve = new AttMetaDropSameFilesForDeltaSieve(_store);
            var attsNotOnDisk = attMetaTransactionResult.ResultFiltered(deltaSieve);

            var funcs = new List<Func<Task<bool>>>();

            foreach (var attMeta in attsNotOnDisk)
            {
                funcs.Add(async () => await SyncAttachmentsHelper(attMeta, metaSyncState, callbackHandler, token));
            }

            return funcs;
        }

        private List<Func<Task<bool>>> SyncCurrentVersionOfContentDocumentsInLibrariesDelta(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, User currentUser, CancellationToken token = default(CancellationToken))
        {
            var sieve = new DocMetaDeltaSieve(_store, currentUser);
            var filesUsedInCategoryOrPrivateFilesMeta = contentDocuments.ResultFiltered(sieve);

            var contentSize = (long)filesUsedInCategoryOrPrivateFilesMeta.Sum(x => x.ContentSize);

            DsaSyncLog.Instance.SetDownloadedFilesInfo(filesUsedInCategoryOrPrivateFilesMeta.Count, contentSize);

            var funcs = new List<Func<Task<bool>>>();

            foreach (var docMeta in filesUsedInCategoryOrPrivateFilesMeta)
            {
                funcs.Add(async () => await SyncCurrentVersionOfContentDocumentsInLibrariesHelper(callbackHandler, docMeta, contentDocuments.SyncState, currentUser, token));
            }

            return funcs;
        }

        private async Task SyncIndexesForContentDocumentsDelta(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, CancellationToken token = default(CancellationToken))
        {
            SendSyncDetailMessage("Indexes for Content Documents", false, false);

            callbackHandler("Sync Indexes for content Documents");
            var sieve = new DocIndexDeltaSieve(_store);
            var documentsWithNewIndexes = await contentDocuments.ResultFilteredAsync(sieve);
            var saveIndexesTasks = documentsWithNewIndexes.Select(doc => SaveSearchIndexesToSoup(doc, token));
            await Task.WhenAll(saveIndexesTasks);

            SendSyncDetailMessage("Indexes for Content Documents", true, false);

            //TODO: Remove from soup tags removed data from salesforce
        }

        private List<Func<Task<bool>>> SyncContentThumbnailsDelta(Action<string> callbackHandler, SyncResult<IList<AttachmentMetadata>> attMetaTransactionResult, CancellationToken token = default(CancellationToken))
        {
            var metaSyncState = attMetaTransactionResult.SyncState;

            // add sieve here
            var deltaSieve = new AttMetaDropUnchangedFilesForDeltaSieve(_store);
            var attsNotOnDisk = attMetaTransactionResult.ResultFiltered(deltaSieve);

            var funcs = new List<Func<Task<bool>>>();

            foreach (var attMeta in attsNotOnDisk)
            {
                funcs.Add(async () => await SyncContentThumbnailsHelper(attMeta, metaSyncState, callbackHandler, metaSyncState, token));
            }
            return funcs;
        }

        #endregion


        #region Sync Attachment/Content Helper Functions

        private async Task<bool> SyncAttachmentsHelper(AttachmentMetadata attMeta, SyncState syncState, Action<string> callbackHandler, CancellationToken token = default(CancellationToken), bool retry = true)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            callbackHandler($"Getting attachment { attMeta.Name } ... on Thread " + Environment.CurrentManagedThreadId);
            var outStream = new AttachmentFileWriter(attMeta, syncState.Id);
            var att = new AttachmentBody(_store, attMeta);

            // Post Detailed Message
            Messenger.Default.Send(new SynchronizationDetailedProgressMessage(attMeta.Name, attMeta.BodyLength));

            var isDownlSucc = false;
            var task = att.SyncDownAndSaveAttachment(callbackHandler, outStream, token);
            var errorMessage = $"Attachment {attMeta.Name} not synced";
            try
            {
                isDownlSucc = await task;
            }
            catch (ErrorResponseException ere)
            {
                if (retry)
                {
                    _funcsToRetry.Add(
                        async () => await SyncAttachmentsHelper(attMeta, syncState, callbackHandler, token, false));
                    return false;
                }
                else
                {
                    errorMessage = ere.Message;
                }
            }
            catch (NetworkErrorException)
            {
                if (retry)
                {
                    _funcsToRetry.Add(
                        async () => await SyncAttachmentsHelper(attMeta, syncState, callbackHandler, token, false));
                    return false;
                }
                else
                {
                    errorMessage = NetworkErrorException.UserFriendlyMessage;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                Debug.WriteLine($"Exception Getting { attMeta.Name } ");
            }

            if (!isDownlSucc)
            {
                var e = new SyncException(errorMessage);
                if (!token.IsCancellationRequested)
                {
                    _funcsToRetry?.Clear();
                    Messenger.Default.Send(new SynchronizationCancelMessage(task, e));
                }
                throw e;
            }
            Messenger.Default.Send(new SynchronizationProgressMessage(attMeta.BodyLength, 0m));
            SuccessfulSyncState.Instance.SaveStateToSoupForAttTransaction(syncState, attMeta.Id);
            return isDownlSucc;
        }

        private async Task<bool> SyncCurrentVersionOfContentDocumentsInLibrariesHelper(Action<string> callbackHandler, Model.Models.ContentDocument docMeta, SyncState syncState, User currentUser, CancellationToken token = default(CancellationToken), bool retry = true)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            var syncPass = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(docMeta.PathOnClient))
                {
                    callbackHandler($"Getting { docMeta.PathOnClient } ... on Thread " + Environment.CurrentManagedThreadId);

                    var outStream = new VersionDataFileWriter(docMeta, syncState.Id);
                    var vd = new VersionData(_store, docMeta);

                    // Post Detailed Message
                    Messenger.Default.Send(new SynchronizationDetailedProgressMessage(docMeta.Title, docMeta.ContentSize));

                    var vdState = false;
                    var task = vd.SyncDownAndSaveVersionData(callbackHandler, outStream, token);
                    var errorMessage = $"Content version { docMeta.Title } not synced";
                    try
                    {
                        vdState = await task;
                    }
                    catch (ErrorResponseException ere)
                    {
                        if (retry)
                        {
                            _funcsToRetry.Add(
                                async () =>
                                    await
                                        SyncCurrentVersionOfContentDocumentsInLibrariesHelper(callbackHandler, docMeta,
                                            syncState, currentUser, token, false));
                            return false;
                        }
                        else
                        {
                            errorMessage = ere.Message;
                        }
                    }
                    catch (NetworkErrorException)
                    {
                        if (retry)
                        {
                            _funcsToRetry.Add(
                                async () =>
                                    await
                                        SyncCurrentVersionOfContentDocumentsInLibrariesHelper(callbackHandler, docMeta,
                                            syncState, currentUser, token, false));
                            return false;
                        }
                        else
                        {
                            errorMessage = NetworkErrorException.UserFriendlyMessage;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                        Debug.WriteLine($"Exception Getting { docMeta.PathOnClient } ");
                    }

                    if (!vdState)
                    {
                        var e = new SyncException(errorMessage);
                        if (!token.IsCancellationRequested)
                        {
                            _funcsToRetry?.Clear();
                            Messenger.Default.Send(new SynchronizationCancelMessage(task, e));
                        }
                        throw e;
                    }
                    Messenger.Default.Send(new SynchronizationProgressMessage(docMeta.ContentSize, 0m));
                }
                syncPass = true;
                SuccessfulSyncState.Instance.SaveStateToSoupForDocTransaction(syncState, docMeta);
            }
            catch (Exception ex)
            {
                throw new SyncException($"CurrentVersion of content documents not synced: { ex.Message }", ex);
            }
            return syncPass;
        }

        private async Task<bool> SyncContentThumbnailsHelper(AttachmentMetadata attMeta, SyncState metaSyncState, Action<string> callbackHandler, SyncState syncState, CancellationToken token = default(CancellationToken), bool retry = true)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            callbackHandler($"Getting content thumbnail { attMeta.Name } ... on Thread " + Environment.CurrentManagedThreadId);
            var outStream = new ContentThumbnailFileWriter(attMeta, syncState.Id);
            var att = new AttachmentBody(_store, attMeta);

            // Post Detailed Message
            Messenger.Default.Send(new SynchronizationDetailedProgressMessage(attMeta.Name, attMeta.BodyLength));

            var isDownlSucc = false;
            var task = att.SyncDownAndSaveAttachment(callbackHandler, outStream, token);
            var errorMessage = $"Content thumbnail {attMeta.Name} not synced";
            try
            {
                isDownlSucc = await task;
            }
            catch (ErrorResponseException ere)
            {
                if (retry)
                {
                    _funcsToRetry.Add(
                        async () => await SyncAttachmentsHelper(attMeta, syncState, callbackHandler, token, false));
                    return false;
                }
                else
                {
                    errorMessage = ere.Message;
                }
            }
            catch (NetworkErrorException)
            {
                if (retry)
                {
                    _funcsToRetry.Add(
                        async () => await SyncAttachmentsHelper(attMeta, syncState, callbackHandler, token, false));
                    return false;
                }
                else
                {
                    errorMessage = NetworkErrorException.UserFriendlyMessage;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                Debug.WriteLine($"Exception Getting { attMeta.Name } ");
            }

            if (!isDownlSucc)
            {
                var e = new SyncException(errorMessage);
                if (!token.IsCancellationRequested)
                {
                    _funcsToRetry?.Clear();
                    Messenger.Default.Send(new SynchronizationCancelMessage(task, e));
                }
                throw e;
            }

            Messenger.Default.Send(new SynchronizationProgressMessage(attMeta.BodyLength, 0m));
            SuccessfulSyncState.Instance.SaveStateToSoupForAttTransaction(metaSyncState, attMeta.Id, attMeta.LastModifiedDate);
            return isDownlSucc;
        }

        #endregion


        #region Other Sync Methods

        private async Task<User> SyncAndGetCurrentUserFromSfdc(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            SyncState currUserState;
            var currUser = new CurrentUser(_store);
            try
            {
                currUserState = await currUser.SyncDownRecords(callbackHandler, token);
                await CheckAndHandleTokenExpiredSyncState(currUserState);
            }
            catch (Exception e)
            {
                if (e?.Message == "Token Expired")
                {
                    throw;
                }
                else
                {
                    CurrentUser.UseAlternativeFields = true;
                    currUserState = await currUser.SyncDownRecords(callbackHandler, token);
                }
            }

            if (currUserState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("CurrentUser not synced");
            }

            var currUserInSoup = currUser.GetCurrentUserFromSoup();

            if (currUserInSoup is NullUser)
            {
                throw new SyncException("CurrentUser not found in soup");
            }

            return currUserInSoup;
        }

        private async Task SaveSearchIndexesToSoup(Model.Models.ContentDocument docMeta, CancellationToken token = default(CancellationToken))
        {
            var documentTag = new ContentDocumentTag(_store);
            var documentTitle = new ContentDocumentTitle(_store);
            var documentAssetType = new ContentDocumentAssetType(_store);
            var documentProductType = new ContentDocumentProductType(_store);

            var indexesTasks = new List<Task>
            {
                documentTag.SaveTagsToSoup(docMeta.Tags, docMeta.Id, token),
                documentTitle.SaveTitleToSoup(docMeta.Title, docMeta.Id, token),
                documentAssetType.SaveAssetTypeToSoup(docMeta.AssetType, docMeta.Id, token),
                documentProductType.SaveProductTypeToSoup(docMeta.ProductType, docMeta.Id, token),
            };
            await Task.WhenAll(indexesTasks);
        }

        private async Task CheckAndHandleTokenExpiredSyncState(SyncState syncState)
        {
            if (syncState.Status == SyncState.SyncStatusTypes.TokenExpired)
            {
                await HandleLogoutTask();
                throw new SyncException("Token Expired");
            }
        }

        #endregion


        #region Other Methods

        /// <summary>
        /// Subscribe to network status chaged event
        /// </summary>
        private void AttachMessages()
        {
            Messenger.Default.Register<NetworkStatusChangedMessage>(this, async (m) =>
            {
                if (m.HasInternetConnection)
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
                    try
                    {
                        using (await _mutex.LockAsync(cts.Token))
                        {
                            await PushPendingData();
                        }
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Network Connection Sync Already In Progress");
                        PlatformAdapter.SendToCustomLogger("Network Connection Sync Already In Progress", LoggingLevel.Error);
                    }
                    finally
                    {
                        try
                        {
                            cts.Dispose();
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine("Network Connection Sync Cancellation Token Already Disposed");
                            PlatformAdapter.SendToCustomLogger("Network Connection Sync Cancellation Token Already Disposed", LoggingLevel.Error);
                        }
                    }
                }
                else
                {
                    Messenger.Default.Send(new SynchronizationCancelMessage(null, new SyncException(NoInternetConnectionMessage)));
                }
            });
        }

        /// <summary>
        /// Get cached account 
        /// Call authentication if no account exist
        /// </summary>
        public async Task<bool> LogInOrGetAccountCached()
        {
            var account = AccountManager.GetAccount();

            if (account == null)
            {
                await PlatformAdapter.Resolve<IAuthHelper>().StartLoginFlowAsync();
            }

            return account != null;
        }

        /// <summary>
        /// Call authentication process 
        /// </summary>
        public async Task LogIn()
        {
            await PlatformAdapter.Resolve<IAuthHelper>().StartLoginFlowAsync();
        }

        /// <summary>
        /// Check internet connection status
        /// </summary>
        public static bool HasInternetConnection()
        {
            var connAvail = NetworkInformation.GetConnectionProfiles().ToList();
            connAvail.Add(NetworkInformation.GetInternetConnectionProfile());

            return connAvail.Any(connection => connection?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }

        /// <summary>
        /// Check internet connection status
        /// throw exception if no connection available 
        /// </summary>
        public static bool HasInternetConnectionOrThrow()
        {
            if (!HasInternetConnection())
            {
                throw new SyncException(NoInternetConnectionMessage);
            }
            return true;
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public async Task<bool> HandleLogoutTask()
        {
            var logoutTaskResult = false;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                logoutTaskResult = await SDKManager.GlobalClientManager.Logout();

                if (logoutTaskResult)
                {
                    //await ClearLocalDataSynced();
                }
            });
            return logoutTaskResult;
        }

        /// <summary>
        /// Clear all local data
        /// </summary>
        private async Task ClearLocalDataSynced()
        {
            _store.ResetDatabase();
            await VersionDataFolder.Instance.DeleteWithContent().ContinueWith((task) =>
            {
                if (task.Exception != null)
                {
                    Debug.WriteLine("Deleting VersionDataFolder end with error");
                    PlatformAdapter.SendToCustomLogger("Deleting VersionDataFolder end with error", LoggingLevel.Error);
                }
            });
            await AttachmentsFolder.Instance.DeleteWithContent().ContinueWith((task) =>
            {
                if (task.Exception != null)
                {
                    Debug.WriteLine("Deleting AttachmentsFolder end with error");
                    PlatformAdapter.SendToCustomLogger("Deleting AttachmentsFolder end with error", LoggingLevel.Error);
                }
            });
            await ContentThumbnailFolder.Instance.DeleteWithContent().ContinueWith((task) =>
            {
                if (task.Exception != null)
                {
                    Debug.WriteLine("Deleting ContentThumbnailFolder end with error");
                    PlatformAdapter.SendToCustomLogger("Deleting ContentThumbnailFolder end with error", LoggingLevel.Error);
                }
            });
        }

        public async Task<bool> NewContentAvaliable(CancellationToken token = default(CancellationToken))
        {
            try
            {
                // Local Variables
                int index = 0;
                int rem = 0;

                // can we sync data
                if (!HasInternetConnection())
                {
                    return false;
                }

                await PushPendingData();

                var account = GetCachedAccount();
                if (account == null)
                {
                    return false;
                }

                // Get the User info
                var syncManager = SyncManager.GetInstance(account);
                var currUser = new CurrentUser(_store);
                var currentUser = await currUser.GetCurrentUserFromSoql(syncManager);
                if (currentUser is NullUser)
                {
                    return false;
                }

                // *** Handle CMC ***
                // Get CMC List from Soup and SOQL
                var cmc = new CategoryMobileConfig(_store, currentUser);
                var cmcModelsFromSoup = cmc.GetFromSoup();
                var cmcModelsFromSOQL = await cmc.GetFromSoql(syncManager);

                // Check CMC Differences
                var newCmcs = cmcModelsFromSOQL.Except(cmcModelsFromSOQL.Join(cmcModelsFromSoup, cc1 => cc1.Id, cc2 => cc2.Id, (cc1, cc2) => cc1)).ToList();
                if (newCmcs.Any())
                {
                    return true;
                }

                // *** Handle CAT ***
                var catIdsFromCmc = cmc.GetCategoryIds();

                // Get Categories List from Soup and SOQL
                var cat = new Category(_store, currentUser);
                var catModelsFromSoql = new List<Model.Models.Category>();
                var catModelsFromSoup = cat.GetFromSoup().ToList();

                // CAT LOOP:  Sync categories in blocks
                index = 0;
                rem = catIdsFromCmc.Count;
                while (rem > 0)
                {
                    int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                    // Get range of Ids to sync
                    cat.CategoryIdList = catIdsFromCmc.GetRange(index, idCount);

                    // Perform Sync
                    var tempResults = await cat.GetFromSoql(syncManager);
                    catModelsFromSoql.AddRange(tempResults);

                    // Iterate to next sublist
                    index += idCount;
                    rem = catIdsFromCmc.Count - index;
                }

                // Check CAT Differences
                var newCats = catModelsFromSoql.Except(catModelsFromSoql.Join(catModelsFromSoup, cc1 => cc1.Id, cc2 => cc2.Id, (cc1, cc2) => cc1)).ToList();
                if (newCats.Any())
                {
                    return true;
                }

                // *** Handle CATCON ***
                var catIdsFromCat = cat.GetIds();

                // Get Categories List from Soup and SOQL
                var catCon = new CategoryContent(_store);
                var catConModelsFromSoql = new List<Model.Models.CategoryContent>();
                var catConModelsFromSoup = catCon.GetFromSoup().ToList();

                // CATCON LOOP:  Sync categoryContent Junctions in blocks
                index = 0;
                rem = catIdsFromCat.Count;
                while (rem > 0)
                {
                    int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                    // Get range of Ids to sync
                    catCon.CategoryIdList = catIdsFromCat.GetRange(index, idCount);

                    // Perform Sync
                    var tempResults = await catCon.GetFromSoql(syncManager);
                    catConModelsFromSoql.AddRange(tempResults);

                    // Iterate to next sublist
                    index += idCount;
                    rem = catIdsFromCat.Count - index;
                }

                // Check CAT Differences
                var newCatCons = catConModelsFromSoql.Except(catConModelsFromSoql.Join(catConModelsFromSoup, cc1 => cc1.Id, cc2 => cc2.Id, (cc1, cc2) => cc1)).ToList();
                if (newCatCons.Any())
                {
                    return true;
                }

                // *** Handle Content Documents ***
                var conDocIdsFromCatCon = catCon.GetContentIds();

                // Get Categories List from Soup and SOQL
                var conDoc = new ContentDocument(_store);
                var conDocModelsFromSoql = new List<Model.Models.ContentDocument>();

                // CONDOC LOOP:  Sync Content Documents in blocks
                index = 0;
                rem = conDocIdsFromCatCon.Count;
                while (rem > 0)
                {
                    int idCount = (rem < kIdListLimit) ? rem : kIdListLimit;

                    // Get range of Ids to sync
                    conDoc.ContentIdList = conDocIdsFromCatCon.GetRange(index, idCount);

                    // Perform Sync
                    var tempResults = await conDoc.GetFromSoql(syncManager);
                    conDocModelsFromSoql.AddRange(tempResults);

                    // Iterate to next sublist
                    index += idCount;
                    rem = conDocIdsFromCatCon.Count - index;
                }

                // Create the Siewve to check for new content
                var sieve = new DocMetaDeltaSieve(_store, currentUser);
                var newConDocs = sieve.GetFilteredResult(conDocModelsFromSoql, catConModelsFromSoql);
                return newConDocs.Any();
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
                // TODO log exception
            }
            return false;
        }

        /// <summary>
        /// Sync up data
        /// </summary>
        private async Task PushPendingData()
        {
            try
            {
                Action<string> callBack = text => Debug.WriteLine(text);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (SfdcConfig.CollectSearchTerms)
                {
                    await SyncUpSearchTerms(callBack);
                }

                await SyncUpEvents(callBack);
                await SyncUpContentReview(callBack);
                await SyncUpPlaylistsAndContent(callBack);
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }

        private async Task CheckAvailableSpace(decimal sizeBytesToDownload)
        {
            sizeBytesToDownload += SfdcConfig.RequiredStorageBufferBytes;
            StorageFolder local = ApplicationData.Current.LocalFolder;
            var retrivedProperties = await local.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace" });
            var sizeBytesAvailable = (UInt64)retrivedProperties["System.FreeSpace"];
            Debug.WriteLine("Bytes to download " + sizeBytesToDownload + " vs capacity " + sizeBytesAvailable);
            if (sizeBytesAvailable < sizeBytesToDownload)
            {
                var s = string.Format("Not enough space to complete sync.  Storage Available: {0}, Storage Required: {1}",
                    MathUtil.BytesToString(sizeBytesAvailable), MathUtil.BytesToString(sizeBytesToDownload));
                SuccessfulSyncState.Instance.RevertSoup(_backupSyncStates);
                throw new SyncException(s);
            }
        }

        private async Task RunAsyncQueueInBatches(List<Func<Task<bool>>> funcs)
        {
            var tasks = new List<Task<bool>>();
            for (var i = Math.Min(funcs.Count() - 1, _concurrentTasksLimit - 1); i > -1; i--)
            {
                DequeueFuncAndRunTask(funcs, i, tasks);
            }
            while (funcs.Any())
            {
                //Stopwatch swTask = new Stopwatch();
                //swTask.Start();

                Task<bool> task = await Task.WhenAny(tasks);
                tasks.Remove(task);
                if (task.IsCanceled || task.IsFaulted)
                {
                    break;
                }
                DequeueFuncAndRunTask(funcs, 0, tasks);
                //swTask.Stop();
            }
            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        private void DequeueFuncAndRunTask(List<Func<Task<bool>>> funcs, int i, List<Task<bool>> tasks)
        {
            Func<Task<bool>> func = funcs[i];
            funcs.RemoveAt(i);
            tasks.Add(Task.Run(func));
        }

        private async Task RunRetryFuncsIfAny()
        {
            if (_funcsToRetry != null)
            {
                foreach (Func<Task<bool>> func in _funcsToRetry)
                {
                    await func();
                }
                _funcsToRetry = null;
            }
        }

        private decimal GetSizeBytesToDownloadAttachmentMetadataHelper(SyncResult<IList<AttachmentMetadata>> syncResult, IResultSieve<IList<AttachmentMetadata>> sieve = null)
        {
            var sizeBytesToDownload = 0m;
            var attMetas = syncResult.ResultFiltered(sieve);

            foreach (var attMeta in attMetas)
            {
                sizeBytesToDownload += attMeta.BodyLength;
            }
            return sizeBytesToDownload;
        }

        private decimal GetSizeBytesToDownloadContentDocumentHelper(SyncResult<IList<Model.Models.ContentDocument>> syncResult, IResultSieve<IList<Model.Models.ContentDocument>> sieve = null)
        {
            var sizeBytesToDownload = 0m;
            var contentDocuments = syncResult.ResultFiltered(sieve);

            foreach (var contentDocument in contentDocuments)
            {
                if (!string.IsNullOrWhiteSpace(contentDocument.PathOnClient))
                {
                    sizeBytesToDownload += contentDocument.ContentSize;
                }
            }
            return sizeBytesToDownload;
        }

        /// <summary>
        /// Get ID of current user
        /// </summary>
        public string GetCurrentUserId()
        {
            var account = GetCachedAccount();
            return account != null
                    ? account.UserId
                    : string.Empty;
        }

        public async Task<bool> IsUserFirstLogIn()
        {
            var account = AccountManager.GetAccount();
            var accountExistsInSoup = LastLoggedAccountState.Instance.CheckIfAccountExistsInSoup(account);

            if (accountExistsInSoup)
            {
                return false;
            }

            try
            {
                await ClearLocalDataSynced();
                LastLoggedAccountState.Instance.SaveStateToSoup(account);
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                Debug.WriteLine($"IsUserFirstLogIn: Database content throw exception");
            }


            return true;
        }

        public async Task<bool> RefreshToken()
        {
            // assign this to a var as ref requires it.
            try
            {
                var account = GetCachedAccount();
                await OAuth2.RefreshAuthToken(account);
                OAuth2.RefreshCookies();
                return true;
            }
            catch (Exception ex)
            {
                // log exception
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
            return false;
        }

        public Account GetCachedAccount()
        {
            return AccountManager.GetAccount();
        }

        private void SendSyncDetailMessage(string input, bool finish, bool up)
        {
            // Sends a detailed message to the UI
            StringBuilder sb = new StringBuilder("Object Sync");
            sb.Append((up) ? " up " : " ");
            sb.Append((finish) ? "Complete for:  " : "Began for:  ");
            sb.Append(input);
            Messenger.Default.Send(new SynchronizationDetailMessage(sb.ToString()));
        }

        #endregion

        #endregion
    }
}
