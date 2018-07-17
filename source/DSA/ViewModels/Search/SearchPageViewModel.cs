using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.UI.Xaml.Controls;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Model.Enums;
using DSA.Model.Messages;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;

namespace DSA.Shell.ViewModels.Search
{
    public class SearchPageViewModel : DSAViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IHistoryDataService _historyDataService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly ISearchContentDataService _searchContentDataService;

        private NavigateToMediaCommand _navigateToMediaCommand;
        private RelayCommand _navigateBackCommand;
        private RelayCommand _searchBoxLostFocusCommand;
        private RelayCommand _searchBoxGotFocusCommand;
        private RelayCommand _prepareForFocusOnKeyboardInput;
        private RelayCommand _querySubmittedCommand;

        private List<MediaLink> _mediaLinks;
        private List<SearchItemViewModel> _results;
        private bool _noResultsMessageVisible;
        private string _searchBoxQueryText;
        private string _noResultsText;
        private bool _focusOnKeyboardInput = true;
        private string _callingPage;

        public SearchPageViewModel(
            IDocumentInfoDataService documentInfoDataService,
            ISearchContentDataService searchContentDataService,
            IHistoryDataService historyDataService,
            INavigationService navigationService,
            ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _navigationService = navigationService;
            _historyDataService = historyDataService;
            _documentInfoDataService = documentInfoDataService;
            _searchContentDataService = searchContentDataService;
         
            Initialize();
        }

        protected override async Task Initialize()
        {
            try
            {
                _navigateToMediaCommand = new NavigateToMediaCommand(_navigationService, _historyDataService);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (SfdcConfig.AppSearchMode == SearchMode.UseSearchPage)
                {
                    var allDocuments = await _documentInfoDataService.GetAllDocuments();
                    _mediaLinks = allDocuments.Select(d => new MediaLink(d)).ToList();
                }
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }

        protected override void AttachMessages()
        {
            base.AttachMessages();
            Messenger.Default.Register<SearchQuerySubmitted>(this, async (m) =>
            {
                SettingsDataService.IsSearchPageVisible = true;
                SearchBoxQueryText = m.Query;
                _callingPage = m.CallingPage;
                await ProcessSearch(m.Query);
            });
        }

        private async Task ProcessSearch(string query)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (SfdcConfig.AppSearchMode != SearchMode.UseSearchPage)
            {
                return;
            }

            var suggestedMedia = await _searchContentDataService.FilterMediaLinkByQuery(_mediaLinks, query);
            Results = suggestedMedia.Select(m => new SearchItemViewModel(m, IsInternalModeEnable)).Where(si => si.IsVisible).ToList();
            NoResultsMessageVisible = Results.Any() == false;
            NoResultsText = $"No results for query: {query}";
        }

        public NavigateToMediaCommand NavigateToMediaCommand => _navigateToMediaCommand;

        public RelayCommand NavigateBackCommand
        {
            get
            {
                return _navigateBackCommand ?? (_navigateBackCommand = new RelayCommand(() =>
                {
                    FocusOnKeyboardInput = true;
                    SettingsDataService.IsSearchPageVisible = false;
                    _navigationService.NavigateTo((string.IsNullOrEmpty(_callingPage)) ? ViewModelLocator.VisualBrowserPageKey : _callingPage);
                }));
            }
        }

        public RelayCommand SearchBoxLostFocusCommand
        {
            get
            {
                return _searchBoxLostFocusCommand ?? (_searchBoxLostFocusCommand = new RelayCommand(
                    () =>
                    {
                        FocusOnKeyboardInput = true;
                    }));
            }
        }

        public RelayCommand SearchBoxGotFocusCommand
        {
            get
            {
                return _searchBoxGotFocusCommand ?? (_searchBoxGotFocusCommand = new RelayCommand(
                    () =>
                    {
                        FocusOnKeyboardInput = false;
                    }));
            }
        }

        public RelayCommand PrepareForFocusOnKeyboardInput
        {
            get
            {
                return _prepareForFocusOnKeyboardInput ?? (_prepareForFocusOnKeyboardInput = new RelayCommand(
                    () =>
                    {
                        FocusOnKeyboardInput = false;
                    }));
            }
        }

        public RelayCommand QuerySubmittedCommand
        {
            get
            {
                return _querySubmittedCommand ?? (_querySubmittedCommand = new RelayCommand(
                    async () =>
                    {
                        await ProcessSearch(SearchBoxQueryText);
                    }));
            }
        }
        public string SearchBoxQueryText
        {
            get { return _searchBoxQueryText; }
            set { Set(ref _searchBoxQueryText, value); }
        }

        public List<SearchItemViewModel> Results
        {
            get { return _results; }
            private set { Set(ref _results, value); }
        }

        public string NoResultsText
        {
            get { return _noResultsText; }
            set { Set(ref _noResultsText, value); }
        }

        public bool NoResultsMessageVisible
        {
            get { return _noResultsMessageVisible; }
            private set
            {
                Set(ref _noResultsMessageVisible, value);
            }
        }

        public bool FocusOnKeyboardInput
        {
            get { return _focusOnKeyboardInput; }
            set { Set(ref _focusOnKeyboardInput, value); }
        }
    }
}
