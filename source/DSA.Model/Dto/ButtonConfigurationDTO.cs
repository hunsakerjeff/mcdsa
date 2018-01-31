using Windows.UI;

namespace DSA.Model.Dto
{
    public class ButtonConfigurationDTO
    {
       public Color TextColor { get; set; }
       public Color HighlightTextColor { get; set; }
       public double Opacity { get; set; }
       public string Image { get; set; }
       public string SelectedImage { get; set; }
    }
}
