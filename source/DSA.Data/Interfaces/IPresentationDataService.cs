using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IPresentationDataService
    {
        bool IsPresentationStarted();

        void StartPresentation(ContactDTO selectedContact);

        void StartPresentationChooseAtCheckOut();

        List<ContentReviewInfo> GetContentReviews();

        Task FinishPresentation(string notes, List<ContentReviewInfo> reviews);

        Task FinishPresentation(ContactDTO contact, string notes, List<ContentReviewInfo> list);

        string GetCurrentContactId();

        void AddContentReview(ContentReviewInfo review);
        bool IsEmailMarked(MediaLink media);
    }
}
