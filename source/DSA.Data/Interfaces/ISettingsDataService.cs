using System.Threading.Tasks;

namespace DSA.Data.Interfaces
{
    public interface ISettingsDataService
    {
        bool IsSearchPageVisible { get; set; }

        Task<string> GetCurrentMobileConfigurationID();

        Task SetCurrentMobileConfigurationID(string id);
        void ClearCurrentMobileConfigurationID();

        bool GetIsInternalModeEnable();
        void SetIsInternalModeEnable(bool isEnable);

        bool InSynchronizationInProgress { get; set; }
    }
}
