using System;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    [JsonObject(Title = "{0}__DSA_Search_Term__c")]
    public class SearchTerms
    {
        [JsonProperty("{0}__Search_Term__c")]
        public string SearchTerm { get; set; }

        [JsonProperty("{0}__Search_Term_Date__c")]
        public DateTime SearchTermDate { get; set; }

        [JsonProperty("{0}__Count__c")]
        public int Count { get; set; }
    }
}
