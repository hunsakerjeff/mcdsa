using Windows.UI.Xaml.Media.Animation;
using DSA.Model.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Shell.Controls.VisualBrowser.ControlBar
{
    public sealed partial class ControlBarControl
    {
        public ControlBarControl()
        {
            this.InitializeComponent();
            Messenger.Default.Register<StartStoryboardMessage>(this, (m) =>
            {
                StartStoryboard(m.StoryboardName, m.LoopForever);
            });

            Messenger.Default.Register<StopStoryboardMessage>(this, (m) =>
            {
                StopStoryboard(m.StoryboardName);
            });
          
        }

        private void StopStoryboard(string storyboardName)
        {
            var storyboard = FindName(storyboardName) as Storyboard;
            storyboard?.Stop();
        }

        private void StartStoryboard(string storyboardName, bool loopForever)
        {
            var storyboard = FindName(storyboardName) as Storyboard;
            if (storyboard != null)
            {

                storyboard.RepeatBehavior = loopForever
                                                ? RepeatBehavior.Forever
                                                : new RepeatBehavior(1);
                storyboard.Begin();
            }
        }
    }
}
