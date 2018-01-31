using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IGeolocationService
    {
        Task<GeolocationInfo> GetLocation();
    }
}
