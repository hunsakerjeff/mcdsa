using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Messages;
using DSA.Sfdc.Sync;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Source.Settings;
using System;

namespace DSA.Shell.ViewModels.Common
{
    public class NewContentBarViewModel : ViewModelBase
    {
        private readonly ISettingsDataService _settingsDataService;
        private readonly IUserSessionService _userSessionService;

        public NewContentBarViewModel(
            IUserSessionService userSessionService,
            ISettingsDataService settingsDataService)
        {
            _userSessionService = userSessionService;
            _settingsDataService = settingsDataService;
            AttachMessages();
        }

        private void AttachMessages()
        {
            Messenger.Default.Register<AppResumingMessage>(this,  async (m) =>
            {
                if (!_settingsDataService.InSynchronizationInProgress && _userSessionService.IsUserLogIn())
                {
                    var hasNewContent = await ObjectSyncDispatcher.Instance.NewContentAvaliable();
                    if (hasNewContent)
                    {
                        SfdcConfig config = (SfdcConfig)SDKManager.ServerConfiguration;
                        if (config.UseAutoAsync)
                        {
                            // requested to not autosync on Fridays
                            if (!_settingsDataService.InSynchronizationInProgress)
                            {
                                Messenger.Default.Send(new SynchronizationAutoStartMessage());
                            }
                        }
                        else
                        {
                            Messenger.Default.Send(new StartStoryboardMessage { StoryboardName = "NewContentAnimation", LoopForever = false });
                            Messenger.Default.Send(new NewContentAvaliableMessage());
                        }
                    }
                }
            });
        }
    }
}