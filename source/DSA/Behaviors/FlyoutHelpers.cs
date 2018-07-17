using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DSA.Util
{
    public static class FlyoutHelpers
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.RegisterAttached("IsOpen", typeof(bool),
            typeof(FlyoutHelpers), new PropertyMetadata(false, OnIsOpenPropertyChanged));

        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.RegisterAttached("Parent", typeof(Button),
            typeof(FlyoutHelpers), new PropertyMetadata(null, OnParentPropertyChanged));

        public static void SetIsOpen(DependencyObject d, bool value)
        {
            d.SetValue(IsOpenProperty, value);
        }

        public static bool GetIsOpen(DependencyObject d)
        {
            return (bool)d.GetValue(IsOpenProperty);
        }

        public static void SetParent(DependencyObject d, Button value)
        {
            d.SetValue(ParentProperty, value);
        }

        public static Button GetParent(DependencyObject d)
        {
            return (Button)d.GetValue(ParentProperty);
        }

        private static void OnParentPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var flyout = d as Flyout;
            if (flyout == null)
            {
                return;
            }

            flyout.Opening += OnFlyoutOpening;
            flyout.Closed += OnFlyoutClosed;
        }

        private static void OnFlyoutClosed(object sender, object e)
        {
            var flyout = sender as Flyout;
            flyout?.SetValue(IsOpenProperty, false);
        }

        private static void OnFlyoutOpening(object sender, object e)
        {
            var flyout = sender as Flyout;
            flyout?.SetValue(IsOpenProperty, true);
        }

        private static void OnIsOpenPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var flyout = d as Flyout;
            var parent = (Button)d.GetValue(ParentProperty);

            if (flyout != null && parent != null)
            {
                var newValue = (bool)e.NewValue;

                // make sure that flyout is opened or showed by dispatcher
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (newValue)
                        flyout.ShowAt(parent);
                    else
                        flyout.Hide();
                }).AsTask();
            }
        }
    }
}
