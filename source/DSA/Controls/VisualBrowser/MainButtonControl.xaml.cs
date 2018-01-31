using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DSA.Shell.Controls.VisualBrowser
{
    public sealed partial class MainButtonControl
    {
        public MainButtonControl()
        {
            this.InitializeComponent();
        }

        private void ButtonImage_OnImageOpened(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            if (img != null) ButtonText.Width = img.ActualWidth;
        }
    }
}
