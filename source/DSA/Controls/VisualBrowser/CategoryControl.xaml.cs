using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace DSA.Shell.Controls.VisualBrowser
{
    public sealed partial class CategoryControl
    {
        public CategoryControl()
        {
            this.InitializeComponent();
            this.DataContextChanged += OnDataContextchange;
        }

        private void OnDataContextchange(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            this.MediaScrollViewer.ScrollToVerticalOffset(0);
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
