using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface ICategoryMobileConfigDataService
    {
        Task<List<CategoryMobileConfig>> GetCategoryMobileConfigs(string mobileAppConfigurationId);
    }
}
