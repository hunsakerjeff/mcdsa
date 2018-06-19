using DSA.ViewModel.Media;
using GalaSoft.MvvmLight.Views;
using Windows.UI.Xaml.Controls;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace DSA.Shell.Controls.Media
{
    public sealed partial class RelatedContentFlyout : SettingsFlyout
    {
        // Properties
        public bool IsOpen { get; private set; }


        // CTOR
        public RelatedContentFlyout()
        {
            this.InitializeComponent();
            this.DataContext = new RelatedContentFlyoutViewModel();
        }

        // Implementation - Public Methods
        public void OpenFlyout()
        {
            if (!IsOpen)
            {
                IsOpen = true;
                this.ShowIndependent();
            }
        }

        public void CloseFlyout()
        {
            if (IsOpen)
            {
                IsOpen = false;
                this.Hide();
            }
        }

        private void SettingsFlyout_BackClick(object sender, BackClickEventArgs e)
        {
            IsOpen = false;
        }
    }
}
