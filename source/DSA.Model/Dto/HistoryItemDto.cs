using System;

namespace DSA.Model.Dto
{
    public class HistoryItemDto
    {
        public MediaLink Media { get; set; }

        public string ConfigurationName { get; set; }

        public DateTime VisitedOn { get; set; }
    }

    public class HistoryInfoDto
    {
        public string Id => $"{ContentDocumentID}-{MobileConfigurationID}";

        public string MobileConfigurationID { get; set; }

        public string ContentDocumentID { get; set; }

        public DateTime VisitedOn { get; set; }
    }
}
