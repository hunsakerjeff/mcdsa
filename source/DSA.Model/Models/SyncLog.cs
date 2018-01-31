using System;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    [JsonObject(Title = "{0}__DSA_Sync_Log__c")]
    public class SyncLog
    {
        [JsonIgnore]
        public string Id { get; set; }

        [JsonProperty("{0}__App_Version__c")]
        public string AppVersion { get; set; }

        [JsonProperty("{0}__Connection_Type__c")]
        public string ConnectionType { get; set; }

        [JsonProperty("{0}__Device_Type__c")]
        public string DeviceType { get; set; }

        [JsonProperty("{0}__Failure_Error_Message__c")]
        public string FailureErrorMessag { get; set; }

        [JsonProperty("{0}__OS_Version__c")]
        public string OsVersion { get; set; }

        [JsonProperty("{0}__Files_to_download__c")]
        public int FilesToDownload { get; set; }

        [JsonProperty("{0}__Size_of_Content__c")]
        public long SizeOfContent { get; set; }

        [JsonProperty("{0}__Sync_Start_Time__c")]
        public DateTime StartTime { get; set; }

        [JsonProperty("{0}__Sync_End_Time__c")]
        public DateTime EndTime { get; set; }

        [JsonProperty("{0}__Sync_Duration__c")]
        public double Duration { get; set; }

        [JsonProperty("{0}__Status__c")]
        public string Status
        {
            get { return SyncStatus.ToString(); }
            set { SyncStatus = (SyncStatus)Enum.Parse(typeof(SyncStatus), value); }
        }

        [JsonProperty("{0}__Sync_Type__c")]
        public string Type
        {
            get { return SyncType.ToString(); }
            set { SyncType = (SyncType)Enum.Parse(typeof(SyncType), value); }
        }

        [JsonProperty("{0}__Username__c")]
        public string Username { get; set; }

        [JsonIgnore]
        public SyncStatus SyncStatus { get; set; }

        [JsonIgnore]
        public SyncType SyncType { get; set; }

    }

    public enum SyncStatus
    {
        Failed,
        Completed
    }

    public enum SyncType
    {
        Initial,
        Delta,
        Full
    }
}
