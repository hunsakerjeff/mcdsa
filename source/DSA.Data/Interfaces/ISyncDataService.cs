using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Models;

namespace DSA.Data.Interfaces
{
    public interface ISyncDataService
    {
        Task<List<SuccessfulSync>> GetSyncConfigs(IEnumerable<string> typeOfObjList);

        Task<List<SuccessfulSync>> GetSyncConfigs(string syncID);

        Task<SuccessfulSync> GetSyncConfig(string typeOfObj);
    }
}
