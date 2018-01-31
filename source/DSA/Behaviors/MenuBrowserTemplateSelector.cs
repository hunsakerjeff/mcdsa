using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DSA.Shell.ViewModels.MenuBrowser;

namespace DSA.Shell.Behaviors
{
    public class MenuBrowserTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CategoryTemplate { get; set; }
        public DataTemplate MediaTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item is CategoryItem
                    ? CategoryTemplate
                    : MediaTemplate;
        }

    }
}
