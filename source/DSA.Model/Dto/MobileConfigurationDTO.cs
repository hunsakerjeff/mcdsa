using System.Collections.Generic;

namespace DSA.Model.Dto
{
    public class MobileConfigurationDTO
    {
        public string Name { get; set; }

        public string PortraitBackgroundImage { get; set; }

        public string LandscapeBackgroundImage { get; set; }

         public string LogoImage { get; set; }

        public ButtonConfigurationDTO ButtonConfiguration { get; set; }

        public IEnumerable<ButtonDTO> Buttons { get; set; }

        public IEnumerable<CategoryBrowserDTO> TopCategories { get; set; }

        public string ReportProblemEmail { get; set; }

        public bool Active { get; set; }
    }
}
