using System.Collections.Generic;
using System.Linq;
using DSA.Model.Dto;
using DSA.Shell.ViewModels.Spotlight;

namespace DSA.Shell.ViewModels.Builders
{
    class SpotlightItemViewModelBuilder
    {
        internal static List<SpotlightGroupList> CreateDefaultGroup(IEnumerable<SpotlightItem> spotLightItems)
        {
            return new List<SpotlightGroupList>
            {
                new SpotlightGroupList(SpotlightGroup.Featured, spotLightItems.Where(sp => sp.Group == SpotlightGroup.Featured).Select(Create)),
                new SpotlightGroupList(SpotlightGroup.NewAndUpdated, spotLightItems.Where(sp => sp.Group == SpotlightGroup.NewAndUpdated).Select(Create))
            };
        }

        private static SpotlightItemViewModel Create(SpotlightItem modelData)
        {
            return new SpotlightItemViewModel(modelData);
        }

        internal static List<SpotlightGroupList> CreateGroupList(List<SpotlightItem> spotLightItems, SpotlightGroup selectedGroup)
        {
            return new List<SpotlightGroupList>
            {
                new SpotlightGroupList(selectedGroup, spotLightItems.Where(sp => sp.Group == selectedGroup).Select(Create)),
            };
        }
    }
}
