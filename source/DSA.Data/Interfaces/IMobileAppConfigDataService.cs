using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface IMobileAppConfigDataService
    {
        Task<List<MobileAppConfig>> GetMobileAppConfigs();
        Task<MobileAppConfig> GetMobileAppConfig(string configID);
    }
}
