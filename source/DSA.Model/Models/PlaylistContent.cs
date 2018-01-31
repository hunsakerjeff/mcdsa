using System;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    [JsonObject(Title = "{0}__Playlist_Content_Junction__c")]
    public class PlaylistContent
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("{0}__Playlist__c")]
        public string PlasylistId { get; set; }

        [JsonProperty("{0}__ContentId__c")]
        public string ContentId { get; set; }

        [JsonProperty("{0}__Order__c")]
        public double? Order { get; set; }

        [JsonProperty("__locally_deleted__")]
        public bool __isDeleted { get; set; }

        [JsonProperty("__local__")]
        public bool __isLocal { get; set; }

        public string ContentId15 => (ContentId?.Length >= 15) ? ContentId.Substring(0, 15) : String.Empty;
    }

}
