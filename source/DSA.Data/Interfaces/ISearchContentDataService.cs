using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface ISearchContentDataService
    {
        Task<IEnumerable<MediaLink>> FilterMediaLinkByQuery(IEnumerable<MediaLink> sourceLinks, string query);
    }
}
