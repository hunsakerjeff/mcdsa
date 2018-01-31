using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IBrowserDataService
    {
        Task<List<BrowserData>> GetBrowserData();
    }
}
