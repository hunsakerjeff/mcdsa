using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model.Enums;
using DSA.Model.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace DSA.Shell.ViewModels.Abstract
{
    public abstract class DSAViewModelBase : ViewModelBase
    {
        private bool _isInternalModeEnable;
        protected ISettingsDataService SettingsDataService;

        protected DSAViewModelBase(
             ISettingsDataService settingsDataService)
        {
            SettingsDataService = settingsDataService;
            AttachMessages();
            InitializeBase();
        }

        protected virtual void AttachMessages()
        {
            Messenger.Default.Register<SynchronizationFinished>(this, (m) =>
           DispatcherHelper.CheckBeginInvokeOnUI(async () =>
           {
               await InitializeBase();
               await Initialize();
               OnAfterSynchronizationFinished(m.Mode, m.AutoSync);
           }));

            Messenger.Default.Register<MobileConfigurationChangedMessage>(this, (m) => 
            DispatcherHelper.CheckBeginInvokeOnUI(async () => 
            {
                await InitializeBase();
                await Initialize();
            }));

            Messenger.Default.Register<UserLogInMessage>(this, (m) => 
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                await InitializeBase();
                await Initialize();
                await OnAfterLogin();
            }));

            Messenger.Default.Register<InternalModeMessage>(this, (m) => IsInternalModeEnable = m.IsEnable);
        }

        protected virtual void OnAfterSynchronizationFinished(SynchronizationMode mode, bool AutoSync)
        {

        }

        protected virtual async Task OnAfterLogin()
        {

        }


        protected virtual async Task Initialize()
        {

        }

        protected virtual async Task InitializeBase()
        {
            try
            {
                IsInternalModeEnable = SettingsDataService.GetIsInternalModeEnable();
            }
            catch
            {
                //todo: log..
            }
        }

        public bool IsInternalModeEnable
        {
            get
            {
                return _isInternalModeEnable;
            }
            set
            {
                Set(ref _isInternalModeEnable, value);
                OnInternalModeChanged(value);
            }
        }

        protected virtual void OnInternalModeChanged(bool value)
        {
            
        }
    }
}