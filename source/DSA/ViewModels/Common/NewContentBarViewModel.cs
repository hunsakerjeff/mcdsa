using DSA.Data.Interfaces;
using DSA.Model.Messages;
using DSA.Sfdc.Sync;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Shell.ViewModels.Common
{
    public class NewContentBarViewModel : ViewModelBase
    {
        private readonly ISettingsDataService _settingsDataService;

        public NewContentBarViewModel(
            ISettingsDataService settingsDataService)
        {
            _settingsDataService = settingsDataService;
            AttachMessages();
        }

        private void AttachMessages()
        {
            Messenger.Default.Register<AppResumingMessage>(this,  async (m) =>
            {
                if (_settingsDataService.InSynchronizationInProgress)
                {
                    return;
                }

                var hasNewContent = await ObjectSyncDispatcher.Instance.NewContentAvaliable();
                if(hasNewContent)
                {
                    Messenger.Default.Send(new StartStoryboardMessage { StoryboardName = "NewContentAnimation", LoopForever = false });
                    Messenger.Default.Send(new NewContentAvaliableMessage());
                }
            });
        }
    }
}