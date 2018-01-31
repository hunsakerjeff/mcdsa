using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface ICategoryContentDataService
    {
        Task<List<CategoryContent>> GetCategoryContent(string mobileConfigurationID);
    }
}
