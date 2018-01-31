using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IUserSettingsDataService
    {
        Task<UserSettingsDto> GetUserSettings(string userID);

        Task SaveSettings(UserSettingsDto userSettings);
    }
}