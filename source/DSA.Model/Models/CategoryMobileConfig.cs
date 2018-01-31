using Newtonsoft.Json;

namespace DSA.Model.Models
{
    public class CategoryMobileConfig
    {
        public static readonly string SoupName = "CategoryMobileConfig";

        public static readonly string MobileAppConfigurationIdIndexKey = $"{SfdcConfig.CustomerPrefix}__MobileAppConfigurationId__c";
        
        public static readonly string CategoryIdIndexKey = $"{SfdcConfig.CustomerPrefix}__CategoryId__c"; 

        public string Id { get; set; }

        [JsonProperty("{0}__Button_Text_Align__c")]
        public string ButtonTextAlign { get; set; }

        [JsonProperty("{0}__CategoryBundleId__c")]
        public string CategoryBundleId { get; set; }

        [JsonProperty("{0}__CategoryId__c")]
        public string CategoryId { get; set; }

        [JsonProperty("{0}__ContentAttachmentId__c")]
        public string ContentAttachmentId { get; set; }

        [JsonProperty("{0}__ContentOverAttachmentId__c")]
        public string ContentOverAttachmentId { get; set; }

        [JsonProperty("{0}__GalleryHeadingTextColor__c")]
        public string GalleryHeadingTextColor { get; set; }

        [JsonProperty("{0}__GalleryHeadingText__c")]
        public string GalleryHeadingText { get; set; }

        [JsonProperty("{0}__IsDefault__c")]
        public bool? IsDefault { get; set; }

        [JsonProperty("{0}__IsDraft__c")]
        public bool? IsDraft { get; set; }

        [JsonProperty("{0}__LandscapeAttachmentId__c")]
        public string LandscapeAttachmentId { get; set; }

        [JsonProperty("{0}__LandscapeX__c")]
        public decimal? LandscapeX { get; set; }

        [JsonProperty("{0}__LandscapeY__c")]
        public decimal? LandscapeY { get; set; }

        [JsonProperty("{0}__MAC_in_Edit__c")]
        public bool? MacInEdit { get; set; }

        [JsonProperty("{0}__MobileAppConfigurationId__c")]
        public string MobileAppConfigurationId { get; set; }

        [JsonProperty("{0}__OverlayBgAlpha__c")]
        public decimal? OverlayBgAlpha { get; set; }

        [JsonProperty("{0}__OverlayBgColor__c")]
        public string OverlayBgColor { get; set; }

        [JsonProperty("{0}__OverlayTextColor__c")]
        public string OverlayTextColor { get; set; }

        [JsonProperty("{0}__PortraitAttachmentId__c")]
        public string PortraitAttachmentId { get; set; }

        [JsonProperty("{0}__PortraitX__c")]
        public decimal? PortraitX { get; set; }

        [JsonProperty("{0}__PortraitY__c")]
        public decimal? PortraitY { get; set; }

        [JsonProperty("{0}__Sub_Category_Background_Color__c")]
        public string SubCategoryBackgroundColor { get; set; }

        [JsonProperty("{0}__Top_Level_Category__c")]
        public string TopLevelCategoryId { get; set; }
    }
}
