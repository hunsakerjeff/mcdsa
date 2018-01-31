using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.Foundation.Diagnostics;
using Windows.Networking.Connectivity;
using Windows.Security.ExchangeActiveSyncProvisioning;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Enums;
using DSA.Model.Models;
using DSA.Sfdc;
using DSA.Sfdc.Sync;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Auth;

namespace DSA.Data.Services.API
{
    public class SyncLogService : ISyncLogService
    {
        public void StartSynchronization(SynchronizationMode syncType)
        {
            if(DsaSyncLog.Instance.IsSyncStarted() || !SfdcConfig.SyncLogsEnable)
                return;

            var account = AccountManager.GetAccount();
            var deviceInfo = new EasClientDeviceInformation();
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            var version = Package.Current.Id.Version;
            var logObject = new SyncLog
            {
                AppVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}",
                DeviceType = deviceInfo.SystemManufacturer + deviceInfo.SystemProductName,
                ConnectionType = GetConnectionType(connectionProfile),
                SyncType = GetSyncType(syncType),
                StartTime = DateTime.UtcNow,
                Username = account.UserName
            };
            DsaSyncLog.Instance.StartSynchronization(logObject);
        }

        public void SynchronizationFailed(Exception exception)
        {
            if(!DsaSyncLog.Instance.IsSyncStarted() || !SfdcConfig.SyncLogsEnable)
                return;

            DsaSyncLog.Instance.SynchronizationStopped(SyncStatus.Failed, exception);
            SyncUpLogs();
        }

        public void SynchronizationCanceled()
        {
            if (!DsaSyncLog.Instance.IsSyncStarted() || !SfdcConfig.SyncLogsEnable)
                return;
            
            DsaSyncLog.Instance.SynchronizationStopped(SyncStatus.Failed);
            SyncUpLogs();
        }

        public void SynchronizationCompleted()
        {
            if (!DsaSyncLog.Instance.IsSyncStarted() || !SfdcConfig.SyncLogsEnable)
                return;
            
            DsaSyncLog.Instance.SynchronizationStopped(SyncStatus.Completed);
            SyncUpLogs();
        }

        private async void SyncUpLogs()
        {
            try
            {
                if (!ObjectSyncDispatcher.HasInternetConnection())
                    return;

                await ObjectSyncDispatcher.Instance.SyncUpDsaSyncLogs((text) => Debug.WriteLine(text));
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                Debug.WriteLine("SyncUpLogs exception");
            }
        }

        private SyncType GetSyncType(SynchronizationMode syncMode)
        {
            switch (syncMode)
            {
                case SynchronizationMode.Full:
                    return SyncType.Full;
                case SynchronizationMode.Delta:
                    return SyncType.Delta;
                case SynchronizationMode.Initial:
                    return SyncType.Initial;
                default:
                    return SyncType.Delta;
            }
        }

        private string GetConnectionType(ConnectionProfile connectionProfile)
        {
            if (connectionProfile != null)
            {
                if (connectionProfile.IsWlanConnectionProfile)
                {
                    return "Wifi";
                }
                return connectionProfile.IsWwanConnectionProfile ? "Mobile" : "Lan";
            }
            return "No Internet";
        }
    }
}
