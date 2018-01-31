using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface ISpotlightDataService
    {
        Task<List<SpotlightItem>> GetSpotlightData();
    }
}
