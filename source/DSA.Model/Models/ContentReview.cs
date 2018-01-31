using Newtonsoft.Json;

namespace DSA.Model.Models
{
    [JsonObject(Title = "{0}__ContentReview__c")]
    public class ContentReview
    {
        [JsonProperty("{0}__ContactId__c")]
        public string ContactId { get; set; }

        [JsonProperty("{0}__ContentId__c")]
        public string ContentId { get; set; }
        
        [JsonProperty("{0}__ContentTitle__c")]
        public string ContentTitle { get; set; }

        [JsonProperty("{0}__Document_Emailed__c")]
        public bool DocumentEmailed { get; set; }

        [JsonProperty("{0}__Geolocation__Latitude__s")]
        public string GeolocationLatitude { get; set; }

        [JsonProperty("{0}__Geolocation__Longitude__s")]
        public string GeolocationLongitude { get; set; }

        [JsonProperty("{0}__Playlist_Id__c")]
        public string PlaylistId { get; set; }

        [JsonProperty("{0}__Rating__c")]
        public int Rating { get; set; }

        [JsonProperty("{0}__TimeViewed__c")]
        public double TimeViewed { get; set; }
    }
}
