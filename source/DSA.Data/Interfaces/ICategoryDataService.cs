using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface ICategoryDataService
    {
        Task<List<Category>> GetCategories(string mobileConfigurationID);
    }
}
