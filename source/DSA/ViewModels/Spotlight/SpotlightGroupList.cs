using System.Collections.Generic;
using System.Linq;
using DSA.Model.Dto;

namespace DSA.Shell.ViewModels.Spotlight
{
    /// <summary>
    /// Spotlight Group List
    /// </summary>
    public class SpotlightGroupList : List<SpotlightItemViewModel>
    {
        public SpotlightGroupList(SpotlightGroup key, IEnumerable<SpotlightItemViewModel> items) : base(items)
        {
            Key = key;
        }

        public SpotlightGroup Key { get; private set; }

        public string DisplayName
        {
            get
            {
                switch(Key)
                {
                    case SpotlightGroup.Featured:
                        return "FEATURED";
                    
                    case SpotlightGroup.NewAndUpdated:
                    default:
                        return "NEW & UPDATED CONTENT";
                }
            }
        }

        public new IEnumerator<object> GetEnumerator()
        {
            return base.GetEnumerator();
        }

        public static SpotlightGroupList Empty(SpotlightGroup key)
        {
            return new SpotlightGroupList(key, Enumerable.Empty<SpotlightItemViewModel>());
        }
    }
}
