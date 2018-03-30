using Windows.UI;
using Windows.UI.Xaml.Media;
using DSA.Model.Dto;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;

namespace DSA.Shell.ViewModels.VisualBrowser
{
    /// <summary>
    /// Category Media ViewModel
    /// - refactoring
    /// </summary>
    public class CategoryMediaViewModel : DSAMediaItemViewModelBase
    {
        #region Fields and Properties

        private Brush _backgroundBrush;
        public ImageSource BackgroundImage
        {
            get;
            private set;
        }

        public Brush BackgroundBrush
        {
            get
            {
                return _backgroundBrush ?? (_backgroundBrush = GetBackGroundBrush());
            }
        }

        public string Name => Media.Name;
        public string Description => Media.Description;
        public string ContentOwner => Media.ContentOwner;
        public string ContentLastUpdatedDate => Media.ContentLastUpdatedDate;
        public string ContentLastReviewedDate => Media.ContentLastReviewedDate;

        #endregion

        #region Constructor

        public CategoryMediaViewModel(
           MediaLink media,
           ImageSource backgroundImage,
           NavigateToMediaCommand navigateToMediaCommand)
        {
            _media = media;
            BackgroundImage = backgroundImage;
            NavigateToMediaCommand = navigateToMediaCommand;
        }

        #endregion

        #region Commands

        public NavigateToMediaCommand NavigateToMediaCommand
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        private Brush GetBackGroundBrush()
        {
            if (BackgroundImage != null)
            {
                return new ImageBrush() { ImageSource = BackgroundImage };
            }

            return new SolidColorBrush(Colors.Black) { Opacity = 0.5 };
        } 

        #endregion
    }
}