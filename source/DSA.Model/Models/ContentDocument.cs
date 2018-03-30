using System;
using Newtonsoft.Json;

namespace DSA.Model.Models
{
    /// <summary>
    /// ContentDocument
    /// https://developer.salesforce.com/docs/atlas.en-us.api.meta/api/sforce_api_objects_contentdocument.htm
    /// </summary>
    public class ContentDocument
    {
        public static string SoupName = "ContentDocument";
        public static string PublishStatusIndexKey = "PublishStatus";
        public static string OwnerIdIndexKey = "OwnerId";

        public string Id { get; set; }
        public string Id15 => Id?.Length >= 15 ? Id.Substring(0, 15) : string.Empty;

        public string LatestPublishedVersionId { get; set; }

        public bool? IsInternal
        {
            get
            {
                bool? res = false;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.IsInternalDocument;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.IsInternalDocument = value;
            }
        }

        public bool? IsAvailableOffline
        {
            get
            {
                bool? res = false;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.IsAvailableOffline;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.IsAvailableOffline = value;
            }
        }

        public string PathOnClient
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.PathOnClient;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.PathOnClient = value;
            }
        }

        public string Description
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.Description;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.Description = value;
            }
        }

        public string DocumentType
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.DocumentType;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.DocumentType = value;
            }
        }

        public string ContentThumbnailId
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.ContentThumbnailId;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.ContentThumbnailId = value;
            }
        }

        public string ContentThumbnailName
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion?.ContentThumbnail != null)
                    res = LatestPublishedVersion.ContentThumbnail.Name;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                var ct = pv.ContentThumbnail ?? new ContentThumbnail();
                ct.Name = value;
            }
        }

        public string ContentUrl
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.ContentUrl;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.ContentUrl = value;
            }
        }

        public string Tags
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.TagCsv;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.TagCsv = value;
            }
        }

        public bool? IsShareable => DocumentType?.Equals("Shareable") ?? false;

        public string Title { get; set; }

        public string FileType { get; set; }

        public decimal ContentSize { get; set; }

        public PublishedVersion LatestPublishedVersion { get; set; }

        public string OwnerId { get; set; }

        public string PublishStatus { get; set; }

        public bool? IsFeatured
        {
            get
            {
                //bool? res = false;
                //if (LatestPublishedVersion.FeaturedContentBoost.HasValue)
                //    res = LatestPublishedVersion.FeaturedContentBoost > 0;
                bool? res = LatestPublishedVersion.IsFeatured ?? false;
                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                //pv.FeaturedContentBoost = value.HasValue && value.Value ? 1 : 0;
                pv.IsFeatured = value.HasValue && value.Value;
            }
        }

        public DateTime? FeaturedContentDate
        {
            get
            {
                DateTime? res = DateTime.MinValue;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.FeaturedContentDate;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.FeaturedContentDate = value;
            }
        }

        public DateTime? PublishedVersionLastModifiedDate
        {
            get
            {
                DateTime? res = DateTime.MinValue;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.LastModifiedDate;

                return res;
            }

            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.FeaturedContentDate = value;
            }
        }

        public string ContentLastUpdatedDate
        {
            get
            {
                if (LatestPublishedVersion != null)
                    return LatestPublishedVersion.ContentLastUpdated;
                return "";
            }

            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.ContentLastUpdated = value;
            }
        }

        public string ContentLastReviewedDate
        {
            get
            {
                return (LatestPublishedVersion != null) ? LatestPublishedVersion.ContentLastReviewed : "";
            }

            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.ContentLastReviewed = value;
            }
        }


        public string ContentOwner
        {
            get
            {
                string res = string.Empty;
                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.ContentOwner?.Name;
                return res;
            }

            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                var ct = pv.ContentOwner ?? new ContentOwner();
                ct.Name = value;
            }
        }

        public string AssetType
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.AssetType;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.AssetType = value;
            }
        }

        public string ProductType
        {
            get
            {
                var res = string.Empty;

                if (LatestPublishedVersion != null)
                    res = LatestPublishedVersion.ProductType;

                return res;
            }
            set
            {
                var pv = LatestPublishedVersion ?? new PublishedVersion();
                pv.ProductType = value;
            }
        }
    }

    /// <summary>
    /// Published Version (ContentVersion)
    /// https://developer.salesforce.com/docs/atlas.en-us.api.meta/api/sforce_api_objects_contentversion.htm
    /// </summary>
    public class PublishedVersion
    {
        public string PathOnClient { get; set; }

        [JsonProperty("{0}__Internal_Document__c")]
        public bool? IsInternalDocument { get; set; }

        [JsonProperty("{0}__Available_Offline__c")]
        public bool? IsAvailableOffline { get; set; }

        public string Description { get; set; }

        [JsonProperty("{0}__Document_Type__c")]
        public string DocumentType { get; set; }

        [JsonProperty("{0}__DSA_Content_Thumbnail__c")]
        public string ContentThumbnailId { get; set; }

        [JsonProperty("{0}__DSA_Content_Thumbnail__r")]
        public ContentThumbnail ContentThumbnail { get; set; }

        public string ContentUrl { get; set; }

        public string TagCsv { get; set; }

        [JsonProperty("{0}__Asset_Type__c")]
        public string AssetType { get; set; }

        [JsonProperty("{0}__Product_Type__c")]
        public string ProductType { get; set; }

        public int? FeaturedContentBoost { get; set; }

        public DateTime? FeaturedContentDate { get; set; }

        [JsonProperty("{0}__IsFeatured__c")]
        public bool? IsFeatured { get; set; }

        [JsonProperty("{0}__Content_Owner__r")]
        public ContentOwner ContentOwner { get; set; }

        [JsonProperty("{0}__Content_Last_Updated__c")]
        public string ContentLastUpdated { get; set; }

        [JsonProperty("Last_Reviewed_Recertified_date__c")]
        public string ContentLastReviewed { get; set; }

        public DateTime? LastModifiedDate { get; set; }
    }

    public class ContentThumbnail
    {
        public string Name { get; set; }
    }

    public class ContentOwner
    {
        public string Name { get; set; }
    }
}
