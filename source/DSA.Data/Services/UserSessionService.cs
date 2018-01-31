using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Sfdc.Sync;

namespace DSA.Data.Services
{
    /// <summary>
    /// User Session Service
    /// </summary>
    public class UserSessionService : IUserSessionService
    {
        /// <summary>
        /// Check if user authenticated
        /// </summary>
        public bool IsUserLogIn()
        {
            return ObjectSyncDispatcher.Instance.GetCachedAccount() != null;
        }

        /// <summary>
        /// Get ID of current user
        /// </summary>
        public string GetCurrentUserId()
        {
            return ObjectSyncDispatcher.Instance.GetCurrentUserId();
        }

        /// <summary>
        /// Check if user authenticed,
        /// call login if no
        /// </summary>
        public async Task LogIn()
        {
            await ObjectSyncDispatcher.Instance.LogInOrGetAccountCached();
        }

        /// <summary>
        ///  Refresh access token
        /// </summary>
        public async Task RefreshToken()
        {
            await ObjectSyncDispatcher.Instance.RefreshToken();
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public async Task<bool> LogOut()
        {
            return await ObjectSyncDispatcher.Instance.HandleLogoutTask();
        }
    }
}
