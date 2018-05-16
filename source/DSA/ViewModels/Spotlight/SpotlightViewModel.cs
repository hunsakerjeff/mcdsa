using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;
using WinRTXamlToolkit.Tools;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar;

namespace DSA.Shell.ViewModels.Spotlight
{
    /// <summary>
    /// Spotlight ViewModel
    /// New, Updated and Featured content
    /// </summary>
    public class SpotlightViewModel : DSAViewModelBase
    {
        #region Fields and Properties

        private readonly INavigationService _navigationService;
        private readonly ISpotlightDataService _spotlightDataService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly ISearchContentDataService _searchContentDataService;
        private SearchControlViewModel _searchViewModel;

        private List<SpotlightItem> _spotLightItems;
        private List<SpotlightGroupList> _source;

        private bool _allFilterSelected = true;
        private bool _featuredFilterSelected;
        private bool _newAndUpdatedFilterSelected;

        public List<SpotlightGroupList> Source
        {
            get
            {
                return _source;
            }
            set
            {
                Set(ref _source, value);
            }
        }

        public bool AllFilterSelected
        {
            get
            {
                return _allFilterSelected;
            }
            set
            {
                Set(ref _allFilterSelected, value);
                if (value)
                {
                    FeaturedFilterSelected = false;
                    NewAndUpdatedFilterSelected = false;
                    SetSource();
                }
            }
        }

        public bool FeaturedFilterSelected
        {
            get
            {
                return _featuredFilterSelected;
            }
            set
            {
                Set(ref _featuredFilterSelected, value);
                if (value)
                {
                    AllFilterSelected = false;
                    NewAndUpdatedFilterSelected = false;
                    SetSource();
                }
            }
        }

        public bool NewAndUpdatedFilterSelected
        {
            get
            {
                return _newAndUpdatedFilterSelected;
            }
            set
            {
                Set(ref _newAndUpdatedFilterSelected, value);
                if (value)
                {
                    AllFilterSelected = false;
                    FeaturedFilterSelected = false;
                    SetSource();
                }
            }
        }

        public SearchControlViewModel SearchViewModel
        {
            get { return _searchViewModel; }
            set { Set(ref _searchViewModel, value); }
        }

        #endregion

        #region Constructor

        public SpotlightViewModel(
                INavigationService navigationService,
                ISpotlightDataService spotlightDataService,
                ISearchContentDataService searchContentDataService,
                IDocumentInfoDataService documentInfoDataService,
                ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _navigationService = navigationService;
            _spotlightDataService = spotlightDataService;
            _documentInfoDataService = documentInfoDataService;
            _searchContentDataService = searchContentDataService;

            Initialize();
        }

        #endregion

        #region Commands

        private NavigateToMediaCommand _navigateToMediaCommand;
        public NavigateToMediaCommand NavigateToMediaCommand
        {
            get
            {
                return _navigateToMediaCommand
                        ?? (_navigateToMediaCommand = new NavigateToMediaCommand(_navigationService));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load data
        /// </summary>
        protected sealed override async Task Initialize()
        {
            try
            {
                // Setup the Search model
                SearchViewModel = new SearchControlViewModel(_documentInfoDataService, _searchContentDataService, _navigationService, NavigateToMediaCommand);

                _spotLightItems = await _spotlightDataService.GetSpotlightData();
                SetSource();
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Internal mode changed
        /// </summary>
        protected override void OnInternalModeChanged(bool value)
        {
            if (_spotLightItems == null)
            {
                return;
            }

            Source = GetSource(FilterInternal(_spotLightItems, value).ToList());
        }

        /// <summary>
        /// Set the source of the list
        /// </summary>
        private void SetSource()
        {
            Source = GetSource(FilterInternal(_spotLightItems, IsInternalModeEnable).ToList());
        }

        private List<SpotlightGroupList> GetSource(List<SpotlightItem> spotLightItems)
        {
            if (AllFilterSelected)
            {
                return SpotlightItemViewModelBuilder.CreateDefaultGroup(spotLightItems);
            }
            else
            {
                var selectedGroup = FeaturedFilterSelected ? SpotlightGroup.Featured : SpotlightGroup.NewAndUpdated;
                return SpotlightItemViewModelBuilder.CreateGroupList(spotLightItems, selectedGroup);
            }
        }

        /// <summary>
        /// Filter internal documents
        /// </summary>
        private IEnumerable<SpotlightItem> FilterInternal(List<SpotlightItem> items, bool isInternalMode)
        {
            items.ForEach(si => si.Media.IsInternalMode = isInternalMode);
            return items.Where(si => si.Media.IsVisible);
        } 

        #endregion
    }
}