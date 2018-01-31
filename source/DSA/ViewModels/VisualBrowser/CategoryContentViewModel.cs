using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Media;
using DSA.Model.Enums;
using GalaSoft.MvvmLight;
using WinRTXamlToolkit.Tools;

namespace DSA.Shell.ViewModels.VisualBrowser
{
    /// <summary>
    /// Category Content ViewModel
    /// - clean
    /// </summary>
    public class CategoryContentViewModel : ViewModelBase
    {
        #region Fields and Properties

        private Brush _backgroundImageBrush;
        private Color _navigationAreaBackgroundColor;
        private Brush _navigationAreaBackground;
        private bool _isInternalMode;
        private ObservableCollection<CategoryMediaViewModel> _media;

        public ImageSource LandscapeImage { get; set; }
        public ImageSource PortraitImage { get; set; }

        public string Header
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public Brush BackgroundImageBrush
        {
            get { return _backgroundImageBrush; }
            private set { Set(ref _backgroundImageBrush, value); }
        }

        public Brush BackgroundBrush
        {
            get
            {
                return new SolidColorBrush { Color = BackgroundColor, Opacity = Opacity };
            }
        }

        public bool IsInternalMode
        {
            get
            {
                return _isInternalMode;
            }
            set
            {
                _isInternalMode = value;
                AllMedia.ForEach(c => c.Media.IsInternalMode = value);
                Media = new ObservableCollection<CategoryMediaViewModel>(AllMedia.Where(ci => ci.Media.IsVisible).OrderBy(o => o.Name));
            }
        }

        public ObservableCollection<CategoryMediaViewModel> Media
        {
            get { return _media; }
            set { Set(ref _media, value); }
        }

        public IEnumerable<CategoryMediaViewModel> AllMedia
        {
            get;
            set;
        }

        public Color BackgroundColor
        {
            get;
            set;
        }

        public double Opacity
        {
            get;
            set;
        }

        public Color NavigationAreaBackgroundColor
        {
            get
            {
                return _navigationAreaBackgroundColor;
            }
            set
            {
                Set(ref _navigationAreaBackgroundColor, value);
                NavigationAreaBackground = new SolidColorBrush(value);
            }
        }

        public Brush NavigationAreaBackground
        {
            get
            {
                return _navigationAreaBackground;
            }
            private set
            {
                Set(ref _navigationAreaBackground, value);
            }
        }

        public ImageSource NavigationAreaImage
        {
            get;
            set;
        }

        public string SubName
        {
            get { return Name.Length > 2 ? Name.Substring(0, 2) : Name; }
        }

        public bool HasImage
        {
            get { return NavigationAreaImage != null; }
        }

        public Brush TextBrush
        {
            get { return new SolidColorBrush(TextColor); }
        }

        public Color TextColor { get; set; }

        public Brush HeaderTextBrush
        {
            get { return new SolidColorBrush(HeaderTextColor); }
        }

        public Color HeaderTextColor { get; set; }

        public int Order { get; internal set; }

        #endregion

        #region Methods

        internal void HandleOrientation(PageOrientations orientation)
        {
            BackgroundImageBrush = ResolveBackgroundBrush(orientation);
        }

        private Brush ResolveBackgroundBrush(PageOrientations orientation)
        {
            switch (orientation)
            {
                case PageOrientations.Portrait:
                case PageOrientations.PortraitFlipped:
                    return PortraitImage != null
                              ? new ImageBrush { ImageSource = PortraitImage }
                              : null;
                //case PageOrientations.Landscape:
                //case PageOrientations.LandscapeFlipped:
                default:
                    return LandscapeImage != null
                             ? new ImageBrush { ImageSource = LandscapeImage }
                             : null;
            }
        }

        #endregion
    }
}