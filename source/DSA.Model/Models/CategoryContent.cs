using Newtonsoft.Json;

namespace DSA.Model.Models
{
    public class CategoryContent
    {
        public static readonly string SoupName = "CategoryContent";

        public static readonly string CategoryIdIndexKey = $"{SfdcConfig.CustomerPrefix}__Category__c"; 

        public string Id { get; set; }

        [JsonProperty("{0}__Category__c")]
        public string CategoryId { get; set; }

        [JsonProperty("{0}__ContentId__c")]
        public string ContentId { get; set; }

        public string ContentId15 => (ContentId?.Length >= 15) ? ContentId.Substring(0, 15) : string.Empty;
    }
}
