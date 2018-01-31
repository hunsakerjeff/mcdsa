using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IHistoryDataService
    {
        Task<List<HistoryItemDto>> GetHistoryData();
        Task AddToHistory(MediaLink media);
    }
}
