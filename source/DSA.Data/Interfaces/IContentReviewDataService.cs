using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface IContentReviewDataService
    {
        Task SaveCompleteReviewToSoup(Model.Dto.MediaLink mediaLink, bool emailed, double viewedTime, string playListId, string contactId);

        Task SaveCompleteReviewsToSoup(List<ContentReview> contentReviews);
       
        void SyncUpContentReview();
    }
}
