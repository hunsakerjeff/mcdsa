using System.Collections.Generic;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    public class Category
    {
        public static readonly string SoupName = "Category";

        public string Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("{0}__Description__c")]
        public string Description { get; set; }

        [JsonProperty("{0}__GalleryAttachmentId__c")]
        public string GalleryAttachmentId { get; set; }

        [JsonProperty("{0}__Is_Parent_Category__c")]
        public bool? IsParentCategory { get; set; }

        [JsonProperty("{0}__Is_Parent__c")]
        public bool? IsParent { get; set; }

        [JsonProperty("{0}__Is_Top_Level__c")]
        public bool? IsTopLevel { get; set; }

        [JsonProperty("{0}__Language__c")]
        public string Language { get; set; }

        [JsonProperty("{0}__Order__c")]
        internal decimal? OrderDecDecimal { get; set; }

        public int Order => (int) (OrderDecDecimal ?? 9999);

        [JsonProperty("{0}__Parent_Category__c")]
        public string ParentCategory { get; set; }
        
        public AttachmentContainer Attachments { get; set; }

        public string ThumbnailAttachmentId => Attachments?.records?.Count > 0 ? Attachments.records[0].Id : null;
    }

    public class AttachmentContainer
    {
        public long totalSize { get; set; }

        public List<Attachment> records { get; set; }
    }

    public class Attachment
    {
        public string Id { get; set; }
    }

}
