using Windows.UI.Xaml.Media;
using DSA.Model.Dto;
using DSA.Model.Enums;
using DSA.Shell.Commands;
using DSA.Shell.Common;

namespace DSA.Shell.ViewModels.MenuBrowser
{
    public class MediaItem : CategoryContentItem, IHaveMedia
    {
        private readonly MediaLink _media;
        private readonly NavigateToMediaCommand _navigationCommand;
        private ImageSource _typeLogo;
        private bool _loadingTypeLogo;

        public MediaItem(
            MediaLink media, 
            NavigateToMediaCommand navigationCommand,
            bool isInternalMode
            ) 
            : base(media.ID, media.Name, isInternalMode)
        {
            _media = media;
            _navigationCommand = navigationCommand;
            IsInternalMode = isInternalMode;
            LoadTypeLogo();
        }

        public ImageSource TypeLogo
        {
            get
            {
                if (_typeLogo == null && _loadingTypeLogo == false)
                {
                    LoadTypeLogo();
                }
                return _typeLogo;
            }
            set
            {
                Set(ref _typeLogo, value);
            }
        }

        public override bool IsInternalMode
        {
            get
            {
                return Media.IsInternalMode;
            }

            set
            {
                Media.IsInternalMode = value;
            }
        }

        public NavigateToMediaCommand NavigateToMediaCommand
        {
            get { return _navigationCommand; }
        }

        public MediaLink Media
        {
            get { return _media; }
        }

        public override bool IsVisible
        {
            get { return Media.IsVisible; }
        }

        public int TypeLogoWidth => 40;

        private async void LoadTypeLogo()
        {
            _loadingTypeLogo = true;
            TypeLogo = await ThumbnailIconResolver.GetIconAsync(Media, TypeLogoWidth);
            _loadingTypeLogo = false;
        }
    }
}
