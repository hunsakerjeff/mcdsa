namespace DSA.Data.Interfaces
{
    public interface INetworkInformationService
    {
        bool HasInternetConnection { get; }
    }
}