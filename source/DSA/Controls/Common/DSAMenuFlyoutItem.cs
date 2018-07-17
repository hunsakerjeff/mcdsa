using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DSA.Shell.Controls.Common
{
    public class DSAMenuFlyoutItem : MenuFlyoutItem
    {
        public new static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(DSAMenuFlyoutItem), new PropertyMetadata(null));

        public new ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
    }
}
