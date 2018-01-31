namespace DSA.Model.Messages
{
    public class NetworkStatusChangedMessage
    {
        public NetworkStatusChangedMessage(bool hasInternetConnection)
        {
            HasInternetConnection = hasInternetConnection;
        }

        public bool HasInternetConnection { get; private set; }
    }
}