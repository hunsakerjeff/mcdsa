using System.Threading.Tasks;

namespace DSA.Data.Interfaces
{
    public interface IUserSessionService
    {
        bool IsUserLogIn();

        Task LogIn();

        Task<bool> LogOut();

        string GetCurrentUserId();

        Task RefreshToken();
    }
}
