using DSA.Model.Models;

namespace DSA.Model.Dto
{
    public class DocumentInfo
    {
        public DocumentInfo(ContentDocument document, SuccessfulSync sync, ContentDistribution contentDistribution)
        {
            Document = document;
            Sync = sync;
            ContentDistribution = contentDistribution;
        }

        public ContentDocument Document { get; set; }
        public SuccessfulSync Sync { get; set; }
        public ContentDistribution ContentDistribution { get; set; }
    }
}
