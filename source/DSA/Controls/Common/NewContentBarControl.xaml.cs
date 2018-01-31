using Windows.UI.Xaml.Media.Animation;
using DSA.Model.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Shell.Controls.Common
{
    public sealed partial class NewContentBarControl
    {
        public NewContentBarControl()
        {
            InitializeComponent();
            Messenger.Default.Register<StartStoryboardMessage>(this, (m) =>
            {
                StartStoryboard(m.StoryboardName, m.LoopForever);
            });
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

        private void NewContentAnimation_Completed(object sender, object e)
        {
            NewContentAnimationHide.Begin();
        }
    }
}
