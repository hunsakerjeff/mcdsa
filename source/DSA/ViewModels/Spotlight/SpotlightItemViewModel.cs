using DSA.Common.Extensions;
using DSA.Model.Dto;
using DSA.Shell.ViewModels.Abstract;

namespace DSA.Shell.ViewModels.Spotlight
{
    /// <summary>
    /// Spotlight Item ViewModel
    /// </summary>
    public class SpotlightItemViewModel : DSAMediaItemViewModelBase
    {
        private readonly SpotlightItem _model;

        public string Name => _model.Name;

        public string Description => _model.Description;

        public string Location => _model.Location;

        public string DateAddedString => _model.AddedOn.HasValue ? _model.AddedOn.Value.ToLongDateString() : string.Empty;

        public SpotlightGroup Group => _model.Group;

        public SpotlightItemViewModel(SpotlightItem model)
        {
            _model = model;
            _media = _model.Media;
        }
    }
}