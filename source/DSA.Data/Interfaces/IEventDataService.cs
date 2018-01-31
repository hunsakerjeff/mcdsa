using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IEventDataService
    {
        Task SaveEventToSoup(string notes, IList<ContentReviewInfo> contentReviews);

        void SyncUpEvents();
    }
}
