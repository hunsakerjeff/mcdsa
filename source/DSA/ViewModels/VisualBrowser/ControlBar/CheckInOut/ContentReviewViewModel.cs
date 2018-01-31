using DSA.Model.Dto;
using DSA.Shell.ViewModels.Abstract;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar.CheckInOut
{
    public class ContentReviewViewModel : DSAMediaItemViewModelBase
    {
        public int _rating;
        private readonly ContentReviewInfo _reviewInfo;

        public ContentReviewViewModel(ContentReviewInfo reviewInfo)
        {
            _reviewInfo = reviewInfo;
            _media = _reviewInfo.MediaLink;
        }

        public string Name => _reviewInfo.MediaLink.Name;
        
        public int Rating
        {
            get
            {
                return _reviewInfo.ContentReview.Rating;
            }
            set
            {
                 _reviewInfo.ContentReview.Rating = value;
                RaisePropertyChanged(nameof(Rating));
            }
        }

        public bool DocumentEmailed
        {
            get
            {
                return _reviewInfo.ContentReview.DocumentEmailed;
            }
            set
            {
                _reviewInfo.ContentReview.DocumentEmailed = value;
                RaisePropertyChanged(nameof(DocumentEmailed));
            }
        }

        public bool IsShareable
        {
            get
            {
                return _reviewInfo.MediaLink.IsShareable;
            }
        }

        public ContentReviewInfo ReviewInfo => _reviewInfo;
    }
}
