using Windows.Networking.Connectivity;
using DSA.Data.Interfaces;
using DSA.Model.Messages;
using DSA.Sfdc.Sync;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Data.Services
{
    public class NetworkInformationService : INetworkInformationService
    {
        public bool HasInternetConnection { get; private set; }

        public NetworkInformationService()
        {
            HasInternetConnection = ObjectSyncDispatcher.HasInternetConnection();
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            var hasInternetAccess = ObjectSyncDispatcher.HasInternetConnection();
            if (HasInternetConnection != hasInternetAccess)
            {
                Messenger.Default.Send(new NetworkStatusChangedMessage(hasInternetAccess));
            }
            HasInternetConnection = hasInternetAccess;
        }
    }
}