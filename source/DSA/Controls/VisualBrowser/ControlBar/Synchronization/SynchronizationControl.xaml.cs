using Windows.Foundation;
using Windows.UI.Xaml;

namespace DSA.Shell.Controls.VisualBrowser.ControlBar.Synchronization
{
    public sealed partial class SynchronizationControl
    {
        public SynchronizationControl()
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

            double actualHorizontalOffset = System.Math.Round(this.MCPopup.HorizontalOffset, 2);
            double actualVerticalOffset = System.Math.Round(this.MCPopup.VerticalOffset, 2);

            double newHorizontalOffset = System.Math.Round((Window.Current.Bounds.Width - gdChild.ActualWidth) / 2 - point.X, 2);
            double newVerticalOffset = System.Math.Round((Window.Current.Bounds.Height - gdChild.ActualHeight) / 2 - point.Y, 2);

            if (actualHorizontalOffset != newHorizontalOffset || actualVerticalOffset != newVerticalOffset)
            {
                this.MCPopup.HorizontalOffset = newHorizontalOffset;
                this.MCPopup.VerticalOffset = newVerticalOffset;
            }
        }

        private void OnGridLayoutUpdated(object sender, object e)
        {
            var point = OverlayPopup.TransformToVisual(null).TransformPoint(new Point(0, 0));

            OverlayGrid.Width = Window.Current.Bounds.Width;
            OverlayGrid.Height = Window.Current.Bounds.Height;

            double actualHorizontalOffset = System.Math.Round(this.OverlayPopup.HorizontalOffset, 2);
            double actualVerticalOffset = System.Math.Round(this.OverlayPopup.VerticalOffset, 2);

            double newHorizontalOffset = System.Math.Round((Window.Current.Bounds.Width - OverlayGrid.ActualWidth) / 2 - point.X, 2);
            double newVerticalOffset = System.Math.Round((Window.Current.Bounds.Height - OverlayGrid.ActualHeight) / 2 - point.Y, 2);

            if (actualHorizontalOffset != newHorizontalOffset || actualVerticalOffset != newVerticalOffset)
            {
                this.OverlayPopup.HorizontalOffset = newHorizontalOffset;
                this.OverlayPopup.VerticalOffset = newVerticalOffset;
            }
        }
    }
}
