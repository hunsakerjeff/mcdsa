using System;

namespace DSA.Model.Dto
{
    public class ContentDistribution
    {
        public string Id { get; set; }

        public string ContentVersionId { get; set; }

        public string DistributionPublicUrl { get; set; }

        public string Name { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}
