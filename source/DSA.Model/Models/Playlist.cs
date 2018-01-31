using Newtonsoft.Json;

namespace DSA.Model.Models
{
    [JsonObject(Title = "{0}__DSA_Playlist__c")]
    public class Playlist
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("{0}__Description__c")]
        public string Description { get; set; }

        [JsonProperty("{0}__IsFeatured__c")]
        public bool? IsFeatured { get; set; }

        public string OwnerId { get; set; }

        [JsonProperty("__locally_deleted__")]
        public bool __isDeleted { get; set; }

        [JsonProperty("__local__")]
        public bool __isLocal { get; set; }

        [JsonProperty("_soupEntryId")]
        public long __localId { get; set; }
    }

}
