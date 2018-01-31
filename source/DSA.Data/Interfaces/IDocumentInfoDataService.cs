using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IDocumentInfoDataService
    {
        Task<List<DocumentInfo>> GetMyLibraryContentDocuments();

        Task<List<DocumentInfo>> GetAllDocuments();

        Task<List<DocumentInfo>> GetContentDocumentsById15(IEnumerable<string> contentID15s);

        Task<List<DocumentInfo>> GetContentDocumentsByID(IEnumerable<string> contentIDs);
    }
}
