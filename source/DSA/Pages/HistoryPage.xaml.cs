using Windows.UI.Xaml.Navigation;

namespace DSA.Shell.Pages
{
    public sealed partial class HistoryPage
    {
        public HistoryPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.historyList.SelectedItem = null;
        }
    }
}
