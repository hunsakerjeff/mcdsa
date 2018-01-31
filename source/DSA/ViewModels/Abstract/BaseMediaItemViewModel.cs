using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using DSA.Model.Dto;
using DSA.Model.Enums;
using DSA.Shell.Common;
using GalaSoft.MvvmLight;

namespace DSA.Shell.ViewModels.Abstract
{
    /// <summary>
    /// Base viewmodel for Media
    /// </summary>
    public abstract class DSAMediaItemViewModelBase : ViewModelBase, IHaveMedia
    {
        #region Fields and Properties

        private ImageSource _icon;
        protected MediaLink _media;
        private bool _loadingIcon;

        public MediaLink Media
        {
            get
            {
                return _media;
            }
        }

        public ImageSource Icon
        {
            get
            {
                if (_icon == null && _loadingIcon != true)
                {
                    LoadIcon();
                }

                return _icon;
            }
            set
            {
                Set(ref _icon, value);
            }
        }

        public virtual int IconWidth => 60;

        #endregion

        #region Methods

        public async Task LoadIcon()
        {
            _loadingIcon = true;
            Icon = await ThumbnailIconResolver.GetIconAsync(Media, IconWidth);
            _loadingIcon = false;
        } 

        #endregion
    }
}
