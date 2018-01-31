﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                        }
                    }
                }
                else { Messenger.Default.Send(new SynchronizationCancelMessage(null, new SyncException(NoInternetConnectionMessage))); }
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
            catch (Exception)
            {
                // log exception
            }
            return false;
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

        public Account GetCachedAccount()
        {
            return AccountManager.GetAccount();
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
                }
            });
            await AttachmentsFolder.Instance.DeleteWithContent().ContinueWith((task) =>
            {
                if (task.Exception != null)
                {
                    Debug.WriteLine("Deleting AttachmentsFolder end with error");
                }
            });
            await ContentThumbnailFolder.Instance.DeleteWithContent().ContinueWith((task) =>
            {
                if (task.Exception != null)
                {
                    Debug.WriteLine("Deleting ContentThumbnailFolder end with error");
                }
            });
        }

        public async Task<bool> NewContentAvaliable(CancellationToken token = default(CancellationToken))
        {
            try
            {
                if (!HasInternetConnection())
                    return false;

                await PushPendingData();

                var account = GetCachedAccount();
                if (account == null)
                    return false;

                var syncManager = SyncManager.GetInstance(account);
                var currUser = new CurrentUser(_store);
                var currentUser = await currUser.GetCurrentUserFromSoql(syncManager);
                if (currentUser is NullUser)
                    return false;

                var contentDocs = new ContentDocument(_store);
                var contentDocumentsResult = await contentDocs.GetContentDocumentsFromSoql(syncManager);

                var categoryContents = new CategoryContent(_store);
                var categoryContentsResult = await categoryContents.GetCategoryContentsFromSoql(syncManager);
                var documentsInCategories = categoryContents.GetAll().ToList();
                var newCategoryContents = categoryContentsResult.Except(categoryContentsResult.Join(documentsInCategories, cc1 => cc1.Id, cc2 => cc2.Id, (cc1, cc2) => cc1)).ToList();

                var sieve = new DocMetaDeltaSieve(_store, currentUser);
                var newContentDocuments = sieve.GetFilteredResult(contentDocumentsResult, categoryContentsResult);
                return newContentDocuments.Any() || newCategoryContents.Any();
            }
            catch (Exception)
            {
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
            catch
            {
                // TODO log exception
            }
        }

        #region Delta Sync

        public async Task<User> QueueingDeltaSyncAsync(Action<string> callbackHandler,
          CancellationToken token = default(CancellationToken))
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

            var syncDownTasks = new List<Task>
            {
                SyncCategories(callbackHandler, currentUser, token),
                SyncCategoryContent(callbackHandler, true, token),
                SyncFeaturedPlaylists(callbackHandler, currentUser, token),
                SyncMobileAppConfigs(callbackHandler, currentUser, token),
                SyncCategoryMobileConfigs(callbackHandler, currentUser, token),
                SyncContacts(callbackHandler, currentUser, token)
            };
            await Task.WhenAll(syncDownTasks);
        }

        public async Task ContentDeltaSyncAsync(Action<string> callbackHandler, User currentUser,
            CancellationToken token = default(CancellationToken))
        {
            var mobileAppConfigsAttMeta = await SyncAndGetMobileAppConfigsAttMeta(callbackHandler, currentUser, token);
            var categoryMobileConfigAttMeta = await SyncAndGetCategoryMobileConfigAttMeta(callbackHandler, currentUser, token);
            var categoryAttMeta = await SyncAndGetCategoryAttMeta(callbackHandler, token);
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

        #region Full Sync

        public async Task<User> QueueingFullSyncAsync(Action<string> callbackHandler,
           CancellationToken token = default(CancellationToken))
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

            var syncDownTasks = new List<Task>
            {
                SyncCategories(callbackHandler, currentUser, token),
                SyncCategoryContent(callbackHandler, false, token),
                SyncFeaturedPlaylists(callbackHandler, currentUser, token),
                SyncMobileAppConfigs(callbackHandler, currentUser, token),
                SyncCategoryMobileConfigs(callbackHandler, currentUser, token),
                SyncContacts(callbackHandler, currentUser, token)
            };

            await Task.WhenAll(syncDownTasks);
        }

        public async Task ContentFullSyncAsync(Action<string> callbackHandler, User currentUser,
            CancellationToken token = default(CancellationToken))
        {
            var mobileAppConfgsAttMeta = await SyncAndGetMobileAppConfigsAttMeta(callbackHandler, currentUser, token);
            var categoryMobileConfigAttMeta = await SyncAndGetCategoryMobileConfigAttMeta(callbackHandler, currentUser, token);
            var categoryAttMeta = await SyncAndGetCategoryAttMeta(callbackHandler, token);
            var syncResult = await SyncAndGetMetadataOfContentDocumentsInLibraries(callbackHandler, token);
            var contentThumbnailAttMeta = await SyncAndGetContentThumbnailAttMeta(callbackHandler, token);

            var sizeBytesToDownload = 0m;
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(mobileAppConfgsAttMeta);
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(categoryMobileConfigAttMeta);
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(categoryAttMeta, new AttMetaDropSameFilesForDeltaSieve(_store));
            sizeBytesToDownload += GetSizeBytesToDownloadContentDocumentHelper(syncResult, new DocMetaByOwnershipAndUsageInCategoriesSieve(_store, currentUser));
            sizeBytesToDownload += GetSizeBytesToDownloadAttachmentMetadataHelper(contentThumbnailAttMeta);

            await CheckAvailableSpace(sizeBytesToDownload);
            Messenger.Default.Send(new SynchronizationProgressMessage(0m, sizeBytesToDownload));

            List<Func<Task<bool>>> funcs = new List<Func<Task<bool>>>();
            _funcsToRetry = new List<Func<Task<bool>>>();
            funcs.AddRange(SyncAttachments(callbackHandler, mobileAppConfgsAttMeta, token));
            funcs.AddRange(SyncAttachments(callbackHandler, categoryMobileConfigAttMeta, token));
            funcs.AddRange(SyncAttachmentsDelta(callbackHandler, categoryAttMeta, token));
            funcs.AddRange(SyncCurrentVersionOfContentDocumentsInLibraries(callbackHandler, syncResult, currentUser, token));
            funcs.AddRange(SyncContentThumbnails(callbackHandler, contentThumbnailAttMeta, token));

            await RunAsyncQueueInBatches(funcs);
            await RunRetryFuncsIfAny();

            await SyncIndexesForContentDocuments(callbackHandler, syncResult, token);
            await SyncMetadataOfContentDistribution(callbackHandler, syncResult, true, token);
        } 

        #endregion

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
                Task<bool> task = await Task.WhenAny(tasks);
                tasks.Remove(task);
                if (task.IsCanceled || task.IsFaulted)
                {
                    break;
                }
                DequeueFuncAndRunTask(funcs, 0, tasks);
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

        private async Task SyncCategoryContent(Action<string> callbackHandler, bool saveNew, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Category Content");
            var category = new CategoryContent(_store);

            var documentsInCategoriesBeforeSync = category.GetAll().ToList();
            var configsState = await category.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("CategoryContent not synced");
            }

            NewCategoryContent.Instance.RecreateClearSoup();
            if (saveNew)
            {
                var documentsInCategoriesAfterSync = category.GetAll().ToList();
                var newCategoryContents = documentsInCategoriesAfterSync.Except(documentsInCategoriesAfterSync.Join(documentsInCategoriesBeforeSync, cc1 => cc1.Id, cc2 => cc2.Id, (cc1, cc2) => cc1)).ToList();
                NewCategoryContent.Instance.SaveToSoup(newCategoryContents);
            }
        }

        private async Task SyncFeaturedPlaylists(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            //get playlist which I own or are marked as featured 
            callbackHandler("Sync Featured Playlists");
            var playlist = new Playlist(_store, currentUser);
            var configsState = await playlist.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlists not synced");
            }

            //get entry subscription it is required to retrieve playlist which are follwed by the user
            callbackHandler("Sync User Subscription");
            var entity = new EntitySubscription(_store, currentUser);
            configsState = await entity.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("User Subscription not synced");
            }

            //get playlist which I follow
            callbackHandler("Sync Followed Playlists");
            playlist.GetPlaylistWhichIFollow = true;
            playlist.DontClearSoup();
            configsState = await playlist.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlists not synced");
            }

            //sync playlist Content
            callbackHandler("Sync Playlist Content");
            var playlistContent = new PlaylistContent(_store, currentUser);
            configsState = await playlistContent.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlist Content not synced");
            }
        }

        public async Task SyncUpPlaylistsAndContent(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            await SyncUpFeaturedPlaylists(callbackHandler, token);
            await SyncUpPlaylistContent(callbackHandler, token);
        }

        public async Task SyncUpFeaturedPlaylists(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync up Featured Playlists");
            var configsState = await Playlist.SyncUpInstance.SyncUpRecords(callbackHandler, token, Playlist.SyncUpInstance.LocalIdChangeHandler);

            await CheckAndHandleTokenExpiredSyncState(configsState);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlists not synced");
            }
        }

        public async Task SyncUpPlaylistContent(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync up Playlist Content");
            var configsState = await PlaylistContent.SyncUpInstance.SyncUpRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Playlist Content not synced");
            }
        }
        private async Task SyncCategoryMobileConfigs(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Category Mobile Configs");
            var configs = new CategoryMobileConfig(_store, currentUser);
            var configsState = await configs.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("MobileAppConfigs not synced");
            }
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

        private async Task<bool> SyncAttachmentsHelper(AttachmentMetadata attMeta, SyncState syncState, Action<string> callbackHandler, CancellationToken token = default(CancellationToken), bool retry = true)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            callbackHandler($"Getting attachment { attMeta.Name } ... on Thread " + Environment.CurrentManagedThreadId);
            var outStream = new AttachmentFileWriter(attMeta, syncState.Id);
            var att = new AttachmentBody(_store, attMeta);

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

        private async Task<bool> SyncContentThumbnailsHelper(AttachmentMetadata attMeta, SyncState metaSyncState, Action<string> callbackHandler, SyncState syncState, CancellationToken token = default(CancellationToken), bool retry = true)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            callbackHandler($"Getting content thumbnail { attMeta.Name } ... on Thread " + Environment.CurrentManagedThreadId);
            var outStream = new ContentThumbnailFileWriter(attMeta, syncState.Id);
            var att = new AttachmentBody(_store, attMeta);

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

        private async Task SyncCategories(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Categories");
            var category = new Category(_store, currentUser);
            var configsState = await category.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("MobileAppConfigs not synced");
            }
        }

        private async Task SyncContacts(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Contacts");
            var category = new Contact(_store, currentUser);
            var configsState = await category.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Contacts not synced");
            }
        }

        private async Task SyncMobileAppConfigs(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Mobile App Config");
            var configs = new MobileAppConfig(_store, currentUser);
            var configsState = await configs.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("MobileAppConfigs not synced");
            }
        }

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetMobileAppConfigsAttMeta(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Metadata Of Mobile App Config");
            var configsAtt = new MobileAppConfigAttachment(_store, currentUser);
            var configsState = await configsAtt.SyncDownRecordsToSoup(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("AttachmentMetadata metadata not synced");
            }

            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.MobileAppConfigs);

            return new SyncResult<IList<AttachmentMetadata>>(configsState, configsAtt.GetMobileAppConfigAttachmentsFromSoup());
        }

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

        private async Task<SyncResult<IList<Model.Models.ContentDocument>>> SyncAndGetMetadataOfContentDocumentsInLibraries(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Metadata Of Content Documents");
            var meta = new ContentDocument(_store);
            var configsState = await meta.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Metadata for content documents not synced");
            }

            // saving metadata successful sync
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.ContentDocuments);

            return new SyncResult<IList<Model.Models.ContentDocument>>(configsState, meta.GetContentDocumentsFromSoup());
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

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetCategoryMobileConfigAttMeta(Action<string> callbackHandler, User currentUser, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Categories Of Mobile App Config");
            var configsAtt = new CategoryMobileConfigAttachment(_store, currentUser);
            var configsState = await configsAtt.SyncDownRecordsToSoup(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("CategoryMobileConfigAttMeta metadata not synced");
            }

            // saving metadata successful sync
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.CategoryMobileConfigs);

            return new SyncResult<IList<AttachmentMetadata>>(configsState, configsAtt.GetCategoryMobileAttachmentsFromSoup());
        }

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetCategoryAttMeta(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Metadata Of Category attachments");
            var configsAtt = new CategoryAttachment(_store);
            var configsState = await configsAtt.SyncDownRecordsToSoup(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("CategoryAttMeta metadata not synced");
            }

            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.MobileAppConfigs);

            return new SyncResult<IList<AttachmentMetadata>>(configsState, configsAtt.GetCategoryAttachmentsFromSoup());
        }

        private async Task<SyncResult<IList<AttachmentMetadata>>> SyncAndGetContentThumbnailAttMeta(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Metadata Of Content Thumbnail attachments");
            var contentThumbnailsAtt = new ContentThumbnailAttachment(_store);
            var contentThumbnailsState = await contentThumbnailsAtt.SyncDownRecordsToSoup(callbackHandler, token);

            if (contentThumbnailsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("ContentThumbnailAttMeta metadata not synced");
            }

            // FIXME: should Content Thumbnails have their own meta type?
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(contentThumbnailsState, SuccessfulSyncState.SyncedObjectMetaType.ContentThumbnails);

            return new SyncResult<IList<AttachmentMetadata>>(contentThumbnailsState, contentThumbnailsAtt.GetContentThumbnailAttachmentsFromSoup());
        }

        private async Task SyncIndexesForContentDocuments(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Indexes for content Documents");
            var saveIndexesTasks = contentDocuments.Result.AsEnumerable().Select(doc => SaveSearchIndexesToSoup(doc, token));
            await Task.WhenAll(saveIndexesTasks);
        }

        private async Task SyncIndexesForContentDocumentsDelta(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync Indexes for content Documents");
            var sieve = new DocIndexDeltaSieve(_store);
            var documentsWithNewIndexes = await contentDocuments.ResultFilteredAsync(sieve);
            var saveIndexesTasks = documentsWithNewIndexes.Select(doc => SaveSearchIndexesToSoup(doc, token));
            await Task.WhenAll(saveIndexesTasks);

            //TODO: Remove from soup tags removed data from salesforce
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

        private async Task SyncMetadataOfContentDistribution(Action<string> callbackHandler, SyncResult<IList<Model.Models.ContentDocument>> contentDocuments, bool fullSync, CancellationToken token = default(CancellationToken))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!SfdcConfig.EmailOnlyContentDistributionLinks)
            {
                return;
            }

            callbackHandler("Sync Metadata Of Content Distribution");
            var meta = new ContentDistribution(_store, fullSync);
            var configsState = await meta.SyncDownRecords(callbackHandler, token);

            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Metadata for content Distribution not synced");
            }
            SuccessfulSyncState.Instance.SaveStateToSoupForMetaTransaction(configsState, SuccessfulSyncState.SyncedObjectMetaType.ContentDistribution);

            await meta.ClearNotNeededContentDistribution(contentDocuments.Result);
        }


        public async Task SyncUpContentReview(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            await Task.Factory.StartNew(async () =>
            {
                if (!HasInternetConnection())
                    return;

                callbackHandler("Sync up for content Reviews");
                var configsState = await ContentReview.Instance.SyncUpContentReview(token);
                await CheckAndHandleTokenExpiredSyncState(configsState);
            }, token);
        }

        public async Task SyncUpEvents(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync up Events");
            var configsState = await Event.Instance.SyncUpRecords(callbackHandler, token);

            await CheckAndHandleTokenExpiredSyncState(configsState);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                throw new SyncException("Events not synced");
            }
        }

        public async Task SyncUpDsaSyncLogs(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            callbackHandler("Sync up DSA SyncLogs");
            var configsState = await DsaSyncLog.Instance.SyncUpRecords(callbackHandler, token);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                //throw new SyncException("DSA SyncLogs not synced");
                callbackHandler("DSA SyncLogs not synced");
            }
        }

        public async Task SyncUpSearchTerms(Action<string> callbackHandler, CancellationToken token = default(CancellationToken))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!SfdcConfig.CollectSearchTerms)
                return;

            callbackHandler("Sync up SearchTerms");
            var searchTerm = new SearchTerm(_store);
            var configsState = await searchTerm.SyncUpRecords(callbackHandler, token);

            await CheckAndHandleTokenExpiredSyncState(configsState);
            if (configsState.Status != SyncState.SyncStatusTypes.Done)
            {
                callbackHandler("SearchTerms not synced");
                //throw new SyncException("SearchTerms not synced");
            }
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
    }
}
