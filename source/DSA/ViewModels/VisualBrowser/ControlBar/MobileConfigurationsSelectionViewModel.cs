using System;
using DSA.Data.Interfaces;
using DSA.Model.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar
{
    public class MobileConfigurationsSelectionViewModel : ViewModelBase
    {
        private readonly MobileAppConfig _appConfig;
        private bool _isSelected;
        private RelayCommand _selectMobileConfigurationCommand;
        private readonly ISettingsDataService _settingsDataService;
        private readonly Action _deselectAction;
        private readonly Action _closePopupAction;

        public MobileConfigurationsSelectionViewModel(
            MobileAppConfig appConfig, 
            bool isSelected,
            ISettingsDataService settingsDataService,
            Action deselectAction, 
            Action closePopupAction)
        {
            _appConfig = appConfig;
            IsSelected = isSelected;
            _settingsDataService = settingsDataService;
            _deselectAction = deselectAction;
            _closePopupAction = closePopupAction;
        }


        public string Name => _appConfig.Title;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        public RelayCommand SelectMobileConfigurationCommand
        {
            get
            {
                return _selectMobileConfigurationCommand ?? (_selectMobileConfigurationCommand = new RelayCommand(
                    async () =>
                    {
                        _deselectAction();
                        await _settingsDataService.SetCurrentMobileConfigurationID(_appConfig.Id);
                        IsSelected = true;
                        _closePopupAction();
                    }));
            }
        }
    }
}