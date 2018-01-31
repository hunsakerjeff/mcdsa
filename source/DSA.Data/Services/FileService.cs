using System;
using System.Threading.Tasks;
using Windows.Storage;
using DSA.Data.Interfaces;
using DSA.Model.Dto;

namespace DSA.Data.Services
{
    public class FileService : IFileService
    {
        public async Task<StorageFile> GetMediaFile(MediaLink media)
        {
            if(media.Type == MediaType.Url)
            {
                return null;
            }

            return await StorageFile.GetFileFromApplicationUriAsync(new Uri(media.Source));
        }
    }
}
