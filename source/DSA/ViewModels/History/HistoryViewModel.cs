using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model.Messages;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;
using WinRTXamlToolkit.Tools;

namespace DSA.Shell.ViewModels.History
{
    public class HistoryViewModel : DSAViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IHistoryDataService _historyDataService;

        public HistoryViewModel(
            IHistoryDataService historyDataService,
            INavigationService navigationService,
            ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _navigationService = navigationService;
            _historyDataService = historyDataService;
            Initialize();
        }

        protected override void AttachMessages()
        {
            base.AttachMessages();
            Messenger.Default.Register<HistoryChangedMessage>(this, (m) =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    await Initialize();
                });
            });
        }
        protected override async Task Initialize()
        {
            try
            {
                var data = await _historyDataService.GetHistoryData();
                _allHistoryItems = HistoryItemViewModelBuilder.Create(data);
                OnInternalModeChanged(IsInternalModeEnable);
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }

        protected override void OnInternalModeChanged(bool value)
        {
            if(_allHistoryItems == null)
            {
                return;
            }

            _allHistoryItems.ForEach(hi => hi.Media.IsInternalMode = value);
            HistoryItems = _allHistoryItems.Where(hi => hi.Media.IsVisible).ToList();
        }

        public List<HistoryItemViewModel> HistoryItems
        {
            get { return _historyItems; }
            private set { Set(ref _historyItems, value); }
        }

        public IEnumerable<HistoryItemViewModel> _allHistoryItems { get; private set; }

        private NavigateToMediaCommand _navigateToMediaCommand;
        private List<HistoryItemViewModel> _historyItems;

        public NavigateToMediaCommand NavigateToMediaCommand
        {
            get
            {
                return _navigateToMediaCommand
                        ?? (_navigateToMediaCommand = new NavigateToMediaCommand(_navigationService) );
            }
        }
    }
}