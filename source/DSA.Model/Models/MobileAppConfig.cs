using System.Collections.Generic;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    public class MobileAppConfig
    {
        public static string SoupName = "MobileAppConfig";

        public string Id { get; set; }

        [JsonProperty("{0}__ButtonHighlightTextColor__c")]
        public string ButtonHighlightTextColor { get; set; }

        [JsonProperty("{0}__ButtonTextAlpha__c")]
        public string ButtonTextAlpha { get; set; }

        [JsonProperty("{0}__ButtonTextColor__c")]
        public string ButtonTextColor { get; set; }

        [JsonProperty("{0}__IntroTextAlpha__c")]
        public string IntroTextAlpha { get; set; }

        [JsonProperty("{0}__IntroTextColor__c")]
        public string IntroTextColor { get; set; }

        [JsonProperty("{0}__IntroText__c")]
        public string IntroText { get; set; }

        [JsonProperty("{0}__TitleBgAlpha__c")]
        public string TitleBgAlpha { get; set; }

        [JsonProperty("{0}__TitleBgColor__c")]
        public string TitleBgColor { get; set; }

        [JsonProperty("{0}__TitleTextAlpha__c")]
        public string TitleTextAlpha { get; set; }

        [JsonProperty("{0}__TitleTextColor__c")]
        public string TitleTextColor { get; set; }
        
        [JsonProperty("{0}__TitleText__c")]
        public string Title { get; set; }

        [JsonProperty("{0}__ButtonDefaultAttachmentId__c")]
        public string ButtonDefaultAttachmentId { get; set; }

        [JsonProperty("{0}__LandscapeAttachmentId__c")]
        public string LandscapeAttachmentId { get; set; }

        [JsonProperty("{0}__PortraitAttachmentId__c")]
        public string PortraitAttachmentId { get; set; }

        [JsonProperty("{0}__LogoAttachmentId__c")]
        public string LogoAttachmentId { get; set; }

        [JsonProperty("{0}__ButtonHighlightAttachmentId__c")]
        public string ButtonHighlightAttachmentId { get; set; }

        [JsonProperty("{0}__Report_an_Issue__c")]
        public string ReportAnIssue { get; set; }

        [JsonProperty("{0}__Active__c")]
        public bool Active { get; set; }
        
        public IList<string> GetAllAttachmentIds()
        {
            return new List<string>()
            {
               ButtonDefaultAttachmentId,
               LandscapeAttachmentId,
               PortraitAttachmentId,
               LogoAttachmentId
            };
            
        }
    }
}
