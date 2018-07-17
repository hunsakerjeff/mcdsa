using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace DSA.Shell.Pages
{
    public sealed partial class SearchPage
    {
        public SearchPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.searchList.SelectedItem = null;
        }
    }
}
