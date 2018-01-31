using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IMobileConfigurationDataService
    {
        Task<ImageSource> GetLogoImage();

        Task<MobileConfigurationDTO> GetCurrentMobileConfiguration();
    }
}
