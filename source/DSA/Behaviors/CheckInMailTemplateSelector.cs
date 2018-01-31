using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar.CheckInOut;

namespace DSA.Shell.Behaviors
{
    public class CheckInMailTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EmailToggleTemplate { get; set; }
        public DataTemplate DocumentRestictedTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var vm = item as ContentReviewViewModel;
            if(vm == null)
            {
                return DocumentRestictedTemplate;
            }
            return vm.IsShareable
                    ? EmailToggleTemplate
                    : DocumentRestictedTemplate;
        }
    }
}
