using System;

namespace DSA.Model.Dto
{
    public class SpotlightItem
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public DateTime? AddedOn { get; set; }

        public SpotlightGroup Group { get; set; }

        public MediaLink Media { get; set; }
    }

    public enum SpotlightGroup
    {
        Featured,
        NewAndUpdated
    }
}
