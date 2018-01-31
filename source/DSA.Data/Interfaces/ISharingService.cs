using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface ISharingService
    {
        Task ShareMedia(MediaLink media);
        Task ShareMedia(List<MediaLink> list, string toEmail);
        void Attach();
    }
}