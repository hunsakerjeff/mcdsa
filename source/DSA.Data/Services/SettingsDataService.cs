using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace DSA.Data.Services
{
    public class SettingsDataService : ISettingsDataService
    {
        private readonly IUserSettingsDataService _userSettingsDataService;

        private string _currentMobileConfigurationID;

        //store in current user settings
        private bool _isInternalModeEnable = false;
        private readonly IUserSessionService _userSessionService;
        private bool _isSearchPageVisible;

        public bool IsSearchPageVisible
        {
            get
            {
                return _isSearchPageVisible;
            }

            set
            {
                _isSearchPageVisible = value;
            }
        }

        public SettingsDataService(
            IUserSettingsDataService userSettingsDataService,
            IUserSessionService userSessionService)
        {
            _userSettingsDataService = userSettingsDataService;
            _userSessionService = userSessionService;
        }

        public async Task<string> GetCurrentMobileConfigurationID()
        {
            var userId = _userSessionService.GetCurrentUserId();
            if(string.IsNullOrEmpty(userId))
            {
                return string.Empty;
            }

            var userSettings = await _userSettingsDataService.GetUserSettings(userId);
            if (userSettings == null)
            {
                return string.Empty;
            }
            _currentMobileConfigurationID = userSettings.SelectedMobileConfigurationId;
            return _currentMobileConfigurationID;
        }

        public bool GetIsInternalModeEnable()
        {
            return _isInternalModeEnable;
        }

        public void SetIsInternalModeEnable(bool isEnable)
        {
            _isInternalModeEnable = isEnable;
            Messenger.Default.Send(new InternalModeMessage { IsEnable = isEnable });
        }

        public async Task SetCurrentMobileConfigurationID(string id)
        {
            if (id != _currentMobileConfigurationID)
            {
                var userId = _userSessionService.GetCurrentUserId();
                await _userSettingsDataService.SaveSettings(new UserSettingsDto { UserId = userId, SelectedMobileConfigurationId = id });
                _currentMobileConfigurationID = id;
                Messenger.Default.Send(new MobileConfigurationChangedMessage(id));
            }
        }

        public void ClearCurrentMobileConfigurationID()
        {
            _currentMobileConfigurationID = null;
        }

        public bool InSynchronizationInProgress
        {
            get;set;
        }
    }
}
