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
using DSA.Shell.Common;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar
{
    public class SearchControlViewModel : ViewModelBase
    {
        private readonly ISearchContentDataService _searchContentDataService;
        private readonly NavigateToMediaCommand _navigateToMediaCommand;
        private readonly INavigationService _navigationService;
        private readonly IDocumentInfoDataService _documentInfoDataService;

        private RelayCommand<SearchBoxResultSuggestionChosenEventArgs> _resultSuggestionChosenCommand;
        private RelayCommand<SearchBoxSuggestionsRequestedEventArgs> _suggestionsRequestedCommand;
        private RelayCommand<SearchBoxQuerySubmittedEventArgs> _querySubmittedCommand;
        private RelayCommand _searchBoxLostFocusCommand;
        private RelayCommand _searchBoxGotFocusCommand;
        private RelayCommand _prepareForFocusOnKeyboardInput;
        private IEnumerable<MediaLink> _mediaLinks;
        private string _searchBoxQueryText;
        private string _firstSuggestionID;
        private IEnumerable<MediaLink> mediaLinks;
        private bool _focusOnKeyboardInput = true;
        private bool _overrideFocusOnKeyboardInput = false;

        public SearchControlViewModel(
            IDocumentInfoDataService documentInfoDataService,
            ISearchContentDataService searchContentDataService,
            INavigationService navigationService,
            NavigateToMediaCommand navigateToMediaCommand)
        {
            _documentInfoDataService = documentInfoDataService;
            _searchContentDataService = searchContentDataService;
            _navigationService = navigationService;
            _navigateToMediaCommand = navigateToMediaCommand;
            Initialize();
            AttachMessages();
        }

        protected async Task Initialize()
        {
            try
            {
                if (SfdcConfig.AppSearchMode == SearchMode.PopulatedInDropDown)
                {
                    var allDocuments = await _documentInfoDataService.GetAllDocuments();
                    _mediaLinks = allDocuments.Select(d => new MediaLink(d));
                }
            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
            }
        }

        private void AttachMessages()
        {
           
            Messenger.Default.Register<IsCheckInFlyoutOpenMessage>(this, (m) =>
             DispatcherHelper.CheckBeginInvokeOnUI(() =>
             {
                 _overrideFocusOnKeyboardInput = m.IsOpen;
                 FocusOnKeyboardInput = m.IsOpen == false;
             }));
        }

        public SearchControlViewModel(NavigateToMediaCommand _navigateToMediaCommand, IEnumerable<MediaLink> mediaLinks, ISearchContentDataService _searchContentDataService, INavigationService _navigationService, IDocumentInfoDataService _documentInfoDataService)
        {
            this._navigateToMediaCommand = _navigateToMediaCommand;
            this.mediaLinks = mediaLinks;
            this._searchContentDataService = _searchContentDataService;
            this._navigationService = _navigationService;
            this._documentInfoDataService = _documentInfoDataService;
        }

        public RelayCommand<SearchBoxResultSuggestionChosenEventArgs> ResultSuggestionChosenCommand
        {
            get
            {
                return _resultSuggestionChosenCommand ?? (_resultSuggestionChosenCommand = new RelayCommand<SearchBoxResultSuggestionChosenEventArgs>(
                    (arg) =>
                    {
                        if(SfdcConfig.AppSearchMode != SearchMode.PopulatedInDropDown)
                        {
                            return;
                        }

                        var mediaLink = _mediaLinks.FirstOrDefault(ml => ml.ID == arg.Tag);
                        _navigateToMediaCommand.Execute(new MediaWrapper { Media = mediaLink });

                    }));
            }
        }

        public RelayCommand<SearchBoxQuerySubmittedEventArgs> QuerySubmittedCommand
        {
            get
            {
                return _querySubmittedCommand ?? (_querySubmittedCommand = new RelayCommand<SearchBoxQuerySubmittedEventArgs>(
                    (arg) =>
                    {
                        if(String.IsNullOrWhiteSpace(arg.QueryText))
                        {
                            return;
                        }

                        if (SfdcConfig.AppSearchMode == SearchMode.UseSearchPage)
                        {
                            if (!_overrideFocusOnKeyboardInput)
                            {
                                FocusOnKeyboardInput = true;
                            }
                            Messenger.Default.Send(new SearchQuerySubmitted { Query = arg.QueryText });
                            _navigationService.NavigateTo(ViewModelLocator.SearchPageKey);
                            return;
                        }

                        if (string.IsNullOrEmpty(_firstSuggestionID))
                        {
                            SearchBoxQueryText = string.Empty;
                            return;
                        }

                        if (!_overrideFocusOnKeyboardInput)
                        {
                            FocusOnKeyboardInput = true;
                        }
                        var mediaLink = _mediaLinks.FirstOrDefault(ml => ml.ID == _firstSuggestionID);
                        _navigateToMediaCommand.Execute(new MediaWrapper { Media = mediaLink });
                    }));
            }
        }

        public RelayCommand<SearchBoxSuggestionsRequestedEventArgs> SuggestionsRequestedCommand
        {
            get
            {
                return _suggestionsRequestedCommand ?? (_suggestionsRequestedCommand = new RelayCommand<SearchBoxSuggestionsRequestedEventArgs>(
                    async (arg) =>
                    {
                        if (string.IsNullOrWhiteSpace(arg.QueryText) 
                            || SfdcConfig.AppSearchMode != SearchMode.PopulatedInDropDown
                            || _mediaLinks == null 
                            || _mediaLinks.Any() == false)
                        {
                            return;
                        }

                        try
                        {
                            var deferral = arg.Request.GetDeferral();

                            await PopulateResults(arg);

                            deferral.Complete();
                        }
                        catch (Exception e)
                        {
                            PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                        }
                    }));
            }
        }

        private async Task PopulateResults(SearchBoxSuggestionsRequestedEventArgs arg)
        {
            var suggestionCollection = arg.Request.SearchSuggestionCollection;

            var suggestedMedia = await _searchContentDataService.FilterMediaLinkByQuery(_mediaLinks, arg.QueryText);
            _firstSuggestionID = suggestedMedia.Select(m => m.ID).FirstOrDefault();
            foreach (var mediaLink in suggestedMedia)
            {
                var imageFile = TypeIconResolver.GetIconStreamReference(mediaLink.Type);
                suggestionCollection.AppendResultSuggestion(mediaLink.Name, mediaLink.Description ?? string.Empty, mediaLink.ID, imageFile, string.Empty);
            }

            if (!suggestedMedia.Any())
            {
                suggestionCollection.AppendQuerySuggestion("No results");
            }
        }

        public RelayCommand SearchBoxLostFocusCommand
        {
            get
            {
                return _searchBoxLostFocusCommand ?? (_searchBoxLostFocusCommand = new RelayCommand(
                    () =>
                    {
                        if (!_overrideFocusOnKeyboardInput)
                        {
                            FocusOnKeyboardInput = true;
                        }
                        SearchBoxQueryText = string.Empty;
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

        public string SearchBoxQueryText
        {
            get { return _searchBoxQueryText; }
            set { Set(ref _searchBoxQueryText, value); }
        }

        public bool FocusOnKeyboardInput
        {
            get { return _focusOnKeyboardInput; }
            set { Set(ref _focusOnKeyboardInput, value); }
        }
    }
}
