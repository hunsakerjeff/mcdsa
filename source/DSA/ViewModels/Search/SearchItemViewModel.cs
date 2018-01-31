using DSA.Model.Dto;
using DSA.Shell.ViewModels.Abstract;

namespace DSA.Shell.ViewModels.Search
{
    public class SearchItemViewModel : DSAMediaItemViewModelBase
    {

        public SearchItemViewModel(MediaLink mediaLink, bool isInternalModeEnable)
        {
            _media = mediaLink;
            _media.IsInternalMode = isInternalModeEnable;
        }

        public string Name => _media.Name;

        public string Description => _media.Description;

        public bool IsVisible => _media.IsVisible;
    }
}
