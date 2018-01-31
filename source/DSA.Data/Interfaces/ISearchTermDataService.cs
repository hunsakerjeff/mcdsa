using System.Threading.Tasks;

namespace DSA.Data.Interfaces
{
    public interface ISearchTermDataService
    {
        Task SaveEventToSoup(string searchTerm);
        void SyncUpSearchTerms();
    }
}