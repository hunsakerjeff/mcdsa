using Windows.UI.Xaml;
using DSA.Shell.ViewModels.MenuBrowser;

namespace DSA.Shell.Controls.MenuBrowser
{
    public sealed partial class MenuBrowserPanel
    {
        public MenuBrowserPanel()
        {
            this.InitializeComponent();
            this.listView.LostFocus += (s, e) =>
            {
                if (listView.SelectedItem is MediaItem)
                {
                    listView.SelectedItem = null;
                }
            };
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(object), typeof(MenuBrowserPanel), new PropertyMetadata(default(object)));

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }


        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(MenuBrowserPanel), new PropertyMetadata(string.Empty));
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }


        public static readonly DependencyProperty CategoryContentProperty = DependencyProperty.Register("CategoryContent", typeof(object), typeof(MenuBrowserPanel), new PropertyMetadata(default(object)));

        public string CategoryContent
        {
            get { return (string)GetValue(CategoryContentProperty); }
            set { SetValue(CategoryContentProperty, value); }
        }
    }
}
