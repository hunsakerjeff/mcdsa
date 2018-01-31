using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using DSA.Sfdc.Sync;

namespace DSA.Shell.Controls.Media
{
    public sealed partial class UrlWebViewControl
    {
        public UrlWebViewControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty UrlSourceProperty = DependencyProperty.RegisterAttached("UrlSource", typeof(string), typeof(UrlWebViewControl), new PropertyMetadata("", OnHtmlPropertyChanged));

        public string UrlSource
        {
            get { return (string)GetValue(UrlSourceProperty); }
            set { SetValue(UrlSourceProperty, value); }
        }

        private static async void OnHtmlPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            if (ObjectSyncDispatcher.HasInternetConnection())
                return;

            var msgDialog = new MessageDialog("Connection Not Available");
            await msgDialog.ShowAsync();
        }
    }
}
