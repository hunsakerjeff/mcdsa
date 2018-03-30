using DSA.Model.Util;
using System;
using DSA.Model.Models;

namespace DSA.Model.Dto
{
    public class MediaLink
    {
        private bool _isInternalMode;

        public MediaLink(DocumentInfo documentInfo, PlaylistContent playlistContent) : this(documentInfo)
        {
            Order = playlistContent.Order;
            JunctionID = playlistContent.Id;
        }

        public MediaLink(DocumentInfo documentInfo)
        {
            var document = documentInfo.Document;
            Document = document;
            ContentDistribution = documentInfo.ContentDistribution;
            ID = document.Id;
            ID15 = document.Id15;
            Name = document.Title;
            Type = MediaTypeResolver.ResolveType(document.FileType, document.PathOnClient);
            Source = Type == MediaType.Url
                        ? document.ContentUrl
                        : $"ms-appdata:///local/VersionData/{document.Id}/{documentInfo.Sync.SyncId}/{document.PathOnClient}";
            Thumbnail =  $"ms-appdata:///local/VersionData/{document.Id}/{documentInfo.Sync.SyncId}/thumbnail.png";
            if (!string.IsNullOrEmpty(document.ContentThumbnailId))
            {
                ContentThumbnail = $"ms-appdata:///local/ContentThumbnails/{document.ContentThumbnailId}/{document.ContentThumbnailName}";
            }
            IsInternal = document.IsInternal ?? false;
            Description = document.Description;
            Order = 0;
            JunctionID = null;
            ContentOwner = document.ContentOwner;
            ContentLastUpdatedDate = document.ContentLastUpdatedDate;
            ContentLastReviewedDate = document.ContentLastReviewedDate;
        }

        public string ID { get; private set; }

        public string ID15 { get; private set; }

        public string Name { get; private set; }

        public double? Order { get; private set; }

        public string JunctionID { get; private set; }

        public string Thumbnail { get; private set; }

        public string ContentThumbnail { get; private set; }

        public string Source { get; private set; }

        public MediaType Type { get; private set; }

        public ContentDocument Document { get; private set; }

        public string Description { get; private set; }

        public bool IsInternal { get; private set; }

        public bool IsInternalMode
        {
            get
            {
                return _isInternalMode;
            }
            set
            {
                _isInternalMode = value;
                IsVisible = IsInternal == false || (IsInternal && value);
            }
        }

        public bool IsVisible { get; internal set; }

        public bool IsShareable
        {
            get
            {
                var isSharable = Document.IsShareable ?? false;

                var canUseContentDistributionLink = ContentDistribution != null 
                                                    && ContentDistribution.IsDeleted == false 
                                                    && (ContentDistribution.ExpiryDate == null || ContentDistribution.ExpiryDate > DateTime.Now);

                if (Type == MediaType.Url)
                {
                    return isSharable;
                }

                return SfdcConfig.EmailOnlyContentDistributionLinks
                                    ? isSharable && canUseContentDistributionLink
                                    : isSharable;
            }
        }

        public string DistributionPublicUrl
        {
            get
            {
                return ContentDistribution != null ? ContentDistribution.DistributionPublicUrl : string.Empty;
            }
        }


        public bool IsSupported
        {
            get
            {
                return Type == MediaType.Html
                        || Type == MediaType.MP4
                        || Type == MediaType.Video
                        || Type == MediaType.PDF
                        || Type == MediaType.Image
                        || Type == MediaType.Audio
                        || Type == MediaType.Url;
            }
        }

        public ContentDistribution ContentDistribution { get; private set; }
        public string ContentOwner { get; private set; }
        public string ContentLastUpdatedDate { get; private set; }
        public string ContentLastReviewedDate { get; private set; }

    }

    public enum MediaType
    {
        Unknow,
        Html,
        MP4,
        PDF,
        Excel,
        Word,
        Image,
        Url,
        PowerPoint,
        Video,
        Visio,
        PSD,
        AI,
        Audio,
        Csv,
        HtmlFile,
        Keynote,
        Pages,
        Rtf,
        Txt,
        Xml,
        Esp
    }
}
