namespace DSA.Model.Dto
{
    public class ButtonDTO
    {
        private readonly CategoryInfo _categoryInfo;

        public ButtonDTO(CategoryInfo categoryInfo)
        {
            _categoryInfo = categoryInfo;
        }

        public string Category => _categoryInfo.Category.Name;

        public decimal PositionXLandscape => _categoryInfo.Config.LandscapeX ?? 0;

        public decimal PositionYLandscape => _categoryInfo.Config.LandscapeY ?? 0;

        public decimal PositionXPortrait => _categoryInfo.Config.PortraitX ?? 0;

        public decimal PositionYPortrait => _categoryInfo.Config.PortraitY ?? 0;
    }
}
