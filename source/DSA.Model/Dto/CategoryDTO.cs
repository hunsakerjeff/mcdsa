using System.Collections.Generic;
using Windows.UI;

namespace DSA.Model.Dto
{
    public class CategoryDTO
    {
        public string Header
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int Order
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string LandscapeImage
        {
            get;
            set;
        }

        public string PortraitImage
        {
            get;
            set;
        }

        public string BackgroundColor
        {
            get;
            set;
        }

        public decimal BackgroundOpacity
        {
            get;
            set;
        }

        public IEnumerable<DocumentInfo> Documents
        {
            get;
            set;
        }

        public string MediaButtonImage
        {
            get;
            set;
        }

        public string SelectedMediaButtonImage
        {
            get;
            set;
        }

        public Color NavigationAreaBackgroundColor
        {
            get;
            set;
        }

        public string NavigationAreaImage
        {
            get;
            set;
        }

        public Color TextColor
        {
            get;
            set;
        }

        public Color HeaderTextColor
        {
            get;
            set;
        }
    }
}
