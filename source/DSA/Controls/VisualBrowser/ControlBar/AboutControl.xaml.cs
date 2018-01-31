using Windows.Foundation;
using Windows.UI.Xaml;

namespace DSA.Shell.Controls.VisualBrowser.ControlBar
{
    public sealed partial class AboutControl
    {
        public AboutControl()
        {
            this.InitializeComponent();
        }

        private void OnLayoutUpdated(object sender, object e)
        {
            var point = MCPopup.TransformToVisual(null).TransformPoint(new Point(0, 0));

            if (gdChild.ActualWidth == 0 && gdChild.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = System.Math.Round(this.MCPopup.HorizontalOffset, 2);
            double ActualVerticalOffset = System.Math.Round(this.MCPopup.VerticalOffset, 2);

            double NewHorizontalOffset = System.Math.Round((Window.Current.Bounds.Width - gdChild.ActualWidth) / 2 - point.X, 2);
            double NewVerticalOffset = System.Math.Round((Window.Current.Bounds.Height - gdChild.ActualHeight) / 2 - point.Y, 2);

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.MCPopup.HorizontalOffset = NewHorizontalOffset;
                this.MCPopup.VerticalOffset = NewVerticalOffset;
            }
        }

        private void OnGridLayoutUpdated(object sender, object e)
        {
            var point = OverlayPopup.TransformToVisual(null).TransformPoint(new Point(0, 0));

            OverlayGrid.Width = Window.Current.Bounds.Width;
            OverlayGrid.Height = Window.Current.Bounds.Height;

            double ActualHorizontalOffset = System.Math.Round(this.OverlayPopup.HorizontalOffset, 2);
            double ActualVerticalOffset = System.Math.Round(this.OverlayPopup.VerticalOffset, 2);

            double NewHorizontalOffset = System.Math.Round((Window.Current.Bounds.Width - OverlayGrid.ActualWidth) / 2 - point.X, 2);
            double NewVerticalOffset = System.Math.Round((Window.Current.Bounds.Height - OverlayGrid.ActualHeight) / 2 - point.Y, 2);

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.OverlayPopup.HorizontalOffset = NewHorizontalOffset;
                this.OverlayPopup.VerticalOffset = NewVerticalOffset;
            }
        }
    }
}
