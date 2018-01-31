namespace DSA.Shell.Controls.VisualBrowser.ControlBar.Synchronization
{
    public sealed partial class SynchronizationStepControl
    {
        public SynchronizationStepControl()
        {
            InitializeComponent();
        }

        private void OnProgresImageUnloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ProgressImageAnimation.Stop();
        }
    }
}
