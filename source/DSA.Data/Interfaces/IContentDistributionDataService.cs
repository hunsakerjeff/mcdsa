using System.Collections.Generic;
using System.Threading.Tasks;

namespace DSA.Data.Interfaces
{
    public interface IContentDistributionDataService
    {
        Task<List<Model.Dto.ContentDistribution>> GetAllContentDistributions();
    }
}