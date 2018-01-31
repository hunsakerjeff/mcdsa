using DSA.Model.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Shell.Pages
{
    public sealed partial class PlaylistsPage
    {
        public PlaylistsPage()
        {
            InitializeComponent();
            Messenger.Default.Register<NewPlaylistAddedMessage>(this, (_) => 
            {
                PlaylistScrollViewer.ScrollToVerticalOffset(PlaylistScrollViewer.ExtentHeight);
            });
        }

     
    }
}
