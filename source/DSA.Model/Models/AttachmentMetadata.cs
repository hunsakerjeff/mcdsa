using System;

namespace DSA.Model.Models
{
    public class AttachmentMetadata
    {
        public string Id { get; set; }
        public string ContentType { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public decimal BodyLength { get; set; }
    }
}
