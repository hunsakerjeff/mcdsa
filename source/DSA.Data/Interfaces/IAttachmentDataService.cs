using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface IAttachmentDataService
    {
          Task<List<AttachmentMetadata>> GetAttachmentsMetadata(IEnumerable<string> attachmentIDs);
    }
}
