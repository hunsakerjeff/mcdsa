using System.Threading.Tasks;
using Salesforce.SDK.SmartSync.Model;

namespace DSA.Sfdc.Sync
{
    public class SyncResult<T>
    {
        public SyncResult(SyncState syncState, T result)
        {
            SyncState = syncState;
            Result = result;
        }
        
        public SyncState SyncState { get; private set; }

        public T Result { get; private set; }

        public T ResultFiltered(IResultSieve<T> sieve)
        {
            T tempRes = Result;

            if (sieve != null && Result != null)
            {
                tempRes = sieve.GetFilteredResult(Result);
            }

            return tempRes;
        }

        public async Task<T> ResultFilteredAsync(IResultSieveAsync<T> sieve)
        {
            T tempRes = Result;

            if (sieve != null && Result != null)
            {
                tempRes = await sieve.GetFilteredResult(Result);
            }

            return tempRes;
        }
    }

    public interface IResultSieve<T>
    {
        T GetFilteredResult(T resultUnfiltered);
    }

    public interface IResultSieveAsync<T>
    {
        Task<T> GetFilteredResult(T resultUnfiltered);
    }
}
