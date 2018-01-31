using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Interactivity;
using WinRTXamlToolkit.Controls.Extensions;

namespace DSA.Util
{
    public class AutoScrollBehavior : Behavior<ScrollViewer>, IBehavior
    {
        private ScrollViewer _scrollViewer = null;
        private double _width = 0.0d;

        DependencyObject IBehavior.AssociatedObject
        {
            get
            {
                return base.AssociatedObject;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            _scrollViewer = base.AssociatedObject;
            _scrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;  
        }

        private void OnScrollViewerLayoutUpdated(object sender, object e)
        {
            if (_scrollViewer.ExtentWidth != _width)
            {
                _scrollViewer.ScrollToHorizontalOffsetWithAnimation(_scrollViewer.ExtentWidth);
                _width = _scrollViewer.ExtentWidth;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_scrollViewer != null)
            {
                _scrollViewer.LayoutUpdated -= OnScrollViewerLayoutUpdated;
            }
        }
    }
}
