using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface IContentDocumentDataService
    {
        Task<List<ContentDocument>> GetMyLibraryContentDocuments();
        Task<List<ContentDocument>> GetContentDocumentsByID15(IEnumerable<string> contentsID);
        Task<List<ContentDocument>> GetContentDocumentsByID(IEnumerable<string> contentIDs);
        Task<List<ContentDocument>> GetAllContentDocuments();
    }
}
