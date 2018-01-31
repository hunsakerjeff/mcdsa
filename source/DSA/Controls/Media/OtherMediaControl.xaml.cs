using System.Windows.Input;
using Windows.UI.Xaml;

namespace DSA.Shell.Controls.Media
{
    public sealed partial class OtherMediaControl
    {
        public OtherMediaControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty OpenInExternalAppCommandProperty = DependencyProperty.Register("OpenInExternalAppCommand", typeof(ICommand), typeof(OtherMediaControl), new PropertyMetadata(null));

        public ICommand OpenInExternalAppCommand
        {
            get { return (ICommand)GetValue(OpenInExternalAppCommandProperty); }
            set { SetValue(OpenInExternalAppCommandProperty, value); }
        }
    }
}
