using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model.Dto;


namespace DSA.Data.Services
{
    public class PresentationDataService : IPresentationDataService
    {
        private bool _isPresentationStarted;
        private ContactDTO _currentContact;
        private List<ContentReviewInfo> _reviews;
        private readonly ISharingService _sharingService;
        private readonly IContentReviewDataService _contentReviewDataService;
        private readonly IEventDataService _eventDataService;

        public PresentationDataService(
            ISharingService sharingService,
            IContentReviewDataService contentReviewDataService,
            IEventDataService eventDataService)
        {
            _sharingService = sharingService;
            _contentReviewDataService = contentReviewDataService;
            _eventDataService = eventDataService;
        }

        public bool IsPresentationStarted()
        {
            return _isPresentationStarted;
        }

        public void StartPresentationChooseAtCheckOut()
        {
            _currentContact = null;
            _isPresentationStarted = true;
            _reviews = new List<ContentReviewInfo>();
        }

        public void StartPresentation(ContactDTO selectedContact)
        {
            _currentContact = selectedContact;
            _isPresentationStarted = true;
            _reviews = new List<ContentReviewInfo>();
        }

        public async Task FinishPresentation(string notes, List<ContentReviewInfo> reviews)
        {
            var mediaToShare = reviews.Where(r => r.ContentReview.DocumentEmailed).Select(r => r.MediaLink);
            if(mediaToShare.Any())
            {
                await _sharingService.ShareMedia(mediaToShare.ToList(), _currentContact.Email);
            }

            reviews.ForEach(cr => cr.ContentReview.ContactId = _currentContact.Id);

            await _contentReviewDataService.SaveCompleteReviewsToSoup(reviews.Select(ci => ci.ContentReview).ToList());
           
            await _eventDataService.SaveEventToSoup(notes, reviews);
            _eventDataService.SyncUpEvents();

            _isPresentationStarted = false;
            _currentContact = null;
            _reviews = new List<ContentReviewInfo>();
        }

        public async Task FinishPresentation(ContactDTO contact, string notes, List<ContentReviewInfo> reviews)
        {
            _currentContact = contact;
            await FinishPresentation(notes, reviews);
        }

        public List<ContentReviewInfo> GetContentReviews()
        {
            return _reviews;
        }

        public void AddContentReview(ContentReviewInfo review)
        {
            if (_isPresentationStarted == false)
            {
                return;
            }

            var oldReview = _reviews.FirstOrDefault(ci => ci.MediaLink.ID15 == review.MediaLink.ID15);
            if (oldReview == null)
            {
                _reviews.Add(review);
                return;
            }
            if(string.IsNullOrEmpty(oldReview.ContentReview.PlaylistId))
            {
                oldReview.ContentReview.PlaylistId = review.ContentReview.PlaylistId;
            }
            oldReview.ContentReview.DocumentEmailed = review.ContentReview.DocumentEmailed;
            oldReview.ContentReview.TimeViewed += review.ContentReview.TimeViewed;
        }

        public string GetCurrentContactId()
        {
            if (_currentContact == null)
            {
                return string.Empty;
            }

            return _currentContact.Id;
        }

        public bool IsEmailMarked(MediaLink media)
        {
            if(_reviews == null)
            {
                return false;
            }

            var review = _reviews.FirstOrDefault(r => r.MediaLink.ID15 == media.ID15);
            if(review == null)
            {
                return false;
            }

            return review.ContentReview.DocumentEmailed;
        }
    }
}
