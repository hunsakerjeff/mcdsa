using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DSA.Shell.ViewModels.Playlist;

namespace DSA.Shell.Behaviors
{
    public class PersonalLibraryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EmptyTemplate { get; set; }
        public DataTemplate HasMediaTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var vm = item as PlaylistViewModel;

            return vm == null || vm.PlayListItems.Any() == false
                    ? EmptyTemplate
                    : HasMediaTemplate;
        }

    }
}
