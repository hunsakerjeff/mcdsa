using Windows.UI.Xaml.Navigation;

namespace DSA.Shell.Pages
{
    public sealed partial class SpotlightPage
    {
        public SpotlightPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.spotlightTable.SelectedItem = null;
        }
    }
}
