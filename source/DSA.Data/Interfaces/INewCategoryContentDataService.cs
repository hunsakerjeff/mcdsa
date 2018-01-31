using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface INewCategoryContentDataService
    {
        Task<List<CategoryContent>> GetCategoryContent(string mobileConfigurationID);
    }
}

