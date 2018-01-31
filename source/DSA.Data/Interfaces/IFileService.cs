using System.Threading.Tasks;
using Windows.Storage;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IFileService
    {
        Task<StorageFile> GetMediaFile(MediaLink media);
    }
}
