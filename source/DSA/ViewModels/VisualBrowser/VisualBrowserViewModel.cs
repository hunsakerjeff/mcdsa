using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.UI.Xaml.Media;
using DSA.Common.Extensions;
using DSA.Common.Utils;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Enums;
using DSA.Model.Messages;
using DSA.Sfdc.Storage;
using DSA.Sfdc.Sync;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;
using WinRTXamlToolkit.Tools;

namespace DSA.Shell.ViewModels.VisualBrowser
{
    /// <summary>
    /// Visual Browser ViewModel
    /// - clean
    /// </summary>
    public class VisualBrowserViewModel : DSAViewModelBase
    {
        #region Fields and Properties

        private readonly IUserSessionService _userSessionService;
        private readonly IPresentationDataService _presentationDataService;
        private readonly ISearchContentDataService _searchContentDataService;
        private readonly ISyncLogService _syncLogService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly IMobileConfigurationDataService _mobileConfigurationDataService;
        private readonly INavigationService _navigationService;
        private readonly IHistoryDataService _historyDataService;
        private readonly IMobileAppConfigDataService _mobileAppConfigDataService;
        private readonly IContactsService _contactsService;
        private readonly IDialogService _dialogService;

        private ImageSource _landscapeImage;
        private ImageSource _portraitImage;
        private ImageSource _backGroundImage;

        private RelayCommand _categoryUnselectedCommand;
        private readonly NavigateToMediaCommand _navigateToMediaCommand;
        private RelayCommand _categoryWithChildrenUnselectedCommand;

        private MobileConfigurationDTO _currentMobileConfiguration;
        private IEnumerable<MainButtonViewModel> _buttons;
        private IEnumerable<CategoryViewModel> _topCategories;
        private bool _isCategorySelected;
        private bool _selectedCategoryHasChildren;

        private ControlBarViewModel _controlBarViewModel;
        private CategoryViewModel _selectedTopCategory;
        private CategoryContentViewModel _selectedCategoryContent;

        private PageOrientations? _orientation;

        private double _topCategoriesMargin;
        private ObservableCollection<CategoryViewModel> _expandedCategories;

        public IEnumerable<CategoryViewModel> TopCategories
        {
            get { return _topCategories; }
            set { Set(ref _topCategories, value); }
        }

        public ImageSource BackgroundImage
        {
            get
            {
                return _backGroundImage;
            }
            set
            {
                Set(ref _backGroundImage, value);
            }
        }

        public double TopCategoriesMargin
        {
            get { return _topCategoriesMargin; }
            set { Set(ref _topCategoriesMargin, value); }
        }

        public ControlBarViewModel ControlBarViewModel
        {
            get { return _controlBarViewModel; }
            set { Set(ref _controlBarViewModel, value); }
        }

        public ObservableCollection<CategoryViewModel> ExpandedCategories
        {
            get { return _expandedCategories; }
            set { Set(ref _expandedCategories, value); }
        }

        public CategoryContentViewModel SelectedCategoryContent
        {
            get { return _selectedCategoryContent; }
            set { Set(ref _selectedCategoryContent, value); }
        }

        public bool SelectedCategoryHasChildren
        {
            get { return _selectedCategoryHasChildren; }
            set
            {
                Set(ref _selectedCategoryHasChildren, value);
                TopCategoriesMargin = value ? -160 : 0;
            }
        }

        public IEnumerable<MainButtonViewModel> Buttons
        {
            get { return _buttons; }
            private set { Set(ref _buttons, value); }
        }

        public bool IsCategorySelected
        {
            get { return _isCategorySelected; }
            set { Set(ref _isCategorySelected, value); }
        }

        public RelayCommand CategoryUnselectedCommand
        {
            get
            {
                return _categoryUnselectedCommand ?? (_categoryUnselectedCommand = new RelayCommand(
                    () => OnCategoryUnselected()));
            }
        }

        public RelayCommand CategoryWithChildrenUnselectedCommand
        {
            get
            {
                return _categoryWithChildrenUnselectedCommand ?? (_categoryWithChildrenUnselectedCommand = new RelayCommand(
                    () =>
                    {
                        SelectedTopCategory = null;
                    }));
            }
        } 

        #endregion

        #region Constructor

        public VisualBrowserViewModel(
          IMobileConfigurationDataService mobileConfigurationDataService,
          INavigationService navigationService,
          IHistoryDataService historyDataService,
          IMobileAppConfigDataService mobileAppConfigDataService,
          ISettingsDataService settingsDataService,
          IDialogService dialogService,
          IUserSessionService userSessionService,
          IContactsService contactsService,
          IPresentationDataService presentationDataService,
          ISearchContentDataService searchContentDataService,
          ISyncLogService syncLogService,
          IDocumentInfoDataService documentInfoDataService) : base(settingsDataService)
        {
            _documentInfoDataService = documentInfoDataService;
            _presentationDataService = presentationDataService;
            _userSessionService = userSessionService;
            _dialogService = dialogService;
            _mobileAppConfigDataService = mobileAppConfigDataService;
            _navigationService = navigationService;
            _historyDataService = historyDataService;
            _mobileConfigurationDataService = mobileConfigurationDataService;
            _navigateToMediaCommand = new NavigateToMediaCommand(_navigationService, _historyDataService);
            _contactsService = contactsService;
            _searchContentDataService = searchContentDataService;
            _syncLogService = syncLogService;

            Initialize();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize Visual Browser
        /// Load all necessary data
        /// </summary>
        protected override async Task Initialize()
        {
            try
            {
                _currentMobileConfiguration = await _mobileConfigurationDataService.GetCurrentMobileConfiguration();

                Buttons = VisualBrowserViewModelBuilder.CreateButtonsViewModel(_currentMobileConfiguration, ShowCategory).ToList();
                TopCategories = VisualBrowserViewModelBuilder.CreateCategoriesViewModel(_currentMobileConfiguration.TopCategories, _navigateToMediaCommand, IsInternalModeEnable, SubCategorySelectionAction);

                ControlBarViewModel = new ControlBarViewModel(_navigationService, _navigateToMediaCommand, _dialogService, SettingsDataService,
                    _mobileAppConfigDataService, _currentMobileConfiguration, _userSessionService, _contactsService,
                    _presentationDataService, _searchContentDataService, _syncLogService, _documentInfoDataService);

                ExpandedCategories = new ObservableCollection<CategoryViewModel>();

                if (_orientation.HasValue)
                {
                    RefreshButtons(_orientation.Value);
                    GetAllCategoryContent().ForEach(c => c.HandleOrientation(_orientation.Value));
                }

                await LoadBackgroundImage();

                if (_orientation.HasValue)
                {
                    BackgroundImage = ResolveBackgroundImage(_orientation.Value);
                }

            }
            catch (Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                // Report error here
            }
        }

        private async Task LoadBackgroundImage()
        {
            if (_landscapeImage == null && _portraitImage == null)
            {
                _landscapeImage = ImageUtil.GetImageSouce(_currentMobileConfiguration.LandscapeBackgroundImage);
                _portraitImage = ImageUtil.GetImageSouce(_currentMobileConfiguration.PortraitBackgroundImage);
            }
            else
            {
                var getLandscape = ImageUtil.GetImageSouceAsync(_currentMobileConfiguration.LandscapeBackgroundImage);
                var getPortrait = ImageUtil.GetImageSouceAsync(_currentMobileConfiguration.PortraitBackgroundImage);

                var results = await Task.WhenAll(getLandscape, getPortrait);
                _landscapeImage = results.FirstOrDefault(t => t.Item1 == _currentMobileConfiguration.LandscapeBackgroundImage)?.Item2;
                _portraitImage = results.FirstOrDefault(t => t.Item1 == _currentMobileConfiguration.PortraitBackgroundImage)?.Item2;
            }
        }

        protected override async Task OnAfterLogin()
        {
            var firstLogin = await ObjectSyncDispatcher.Instance.IsUserFirstLogIn();
            if (firstLogin)
            {
                if (ControlBarViewModel.ShowInitialynchronizationPopup.CanExecute(this))
                {
                    ControlBarViewModel.ShowInitialynchronizationPopup.Execute(this);
                }
            }
            else
            {
                if (ObjectSyncDispatcher.HasInternetConnection())
                {
                    if (ControlBarViewModel.ShowDeltaSynchronizationPopup.CanExecute(this))
                    {
                        ControlBarViewModel.ShowDeltaSynchronizationPopup.Execute(this);
                    }
                }
            }
        }

        /// <summary>
        /// Handle internal mode changed
        /// </summary>
        /// <param name="value"></param>
        protected override void OnInternalModeChanged(bool value)
        {
            base.OnInternalModeChanged(value);
            GetAllCategoryContent().ForEach(c => c.IsInternalMode = value);
        }

        private List<CategoryContentViewModel> GetAllCategoryContent()
        {
            if (TopCategories == null)
            {
                return new List<CategoryContentViewModel>();
            }

            return GetContentAndChildrenContentCategories(TopCategories);
        }

        private List<CategoryContentViewModel> GetContentAndChildrenContentCategories(IEnumerable<CategoryViewModel> categories)
        {
            return categories
                    .Select(t => t.Content)
                    .ToList()
                    .AppendList(categories.SelectMany(tc => GetContentAndChildrenContentCategories(tc.AllChildren)));
        }

        protected override void AttachMessages()
        {
            base.AttachMessages();
            Messenger.Default.Register<OrientationStateMessage>(
                this,
                m =>
                {
                    _orientation = m.Orientation;
                    HandleOrientation(m.Orientation);
                });
            Messenger.Default.Register<NavigateVisualBrowserTopLevelMessage>(
                this,
                m =>
                {
                    if (IsCategorySelected)
                    {
                        OnCategoryUnselected();
                    }
                });
        }

        protected override void OnAfterSynchronizationFinished(SynchronizationMode mode)
        {
            if (mode == SynchronizationMode.Initial)
            {
                SettingsDataService.ClearCurrentMobileConfigurationID();
                ControlBarViewModel.IsSelectConfigurationPopupOpen = true;
            }
            else
            {
                _dialogService.ShowMessage("Synchronization completed successfully", "Synchronization completed");
            }
            Task.Factory.StartNew(AttachmentsFolder.Instance.DeleteOldVersionsWithContent);
            Task.Factory.StartNew(VersionDataFolder.Instance.DeleteOldVersionsWithContent);
        }

        private void ShowCategory(string name)
        {
            var firstCategory = TopCategories.FirstOrDefault(c => c.Name == name);
            IsCategorySelected = firstCategory != null;
            SelectedTopCategory = firstCategory;
        }

        private void HandleOrientation(PageOrientations orientation)
        {
            BackgroundImage = ResolveBackgroundImage(orientation);

            RefreshButtons(orientation);

            GetAllCategoryContent().ForEach(c => c.HandleOrientation(orientation));
        }

        private void RefreshButtons(PageOrientations orientation)
        {
            Buttons?.ToList().ForEach(b => b.HandleOrientation(orientation));
        }

        private ImageSource ResolveBackgroundImage(PageOrientations orientation)
        {
            switch (orientation)
            {
                case PageOrientations.Landscape:
                case PageOrientations.LandscapeFlipped:
                    return _landscapeImage;
                case PageOrientations.Portrait:
                case PageOrientations.PortraitFlipped:
                default:
                    return _portraitImage;
            }
        }

        private void OnCategoryUnselected()
        {
            IsCategorySelected = false;
            SelectedTopCategory = null;
            foreach (var button in Buttons)
            {
                button.Deselect();
            }
        }

        private void SubCategorySelectionAction(CategoryViewModel categoryVm)
        {
            SelectedCategoryContent = categoryVm.Content;
            var maxLevel = ExpandedCategories.Select(ci => ci.Level).Max();
            if (categoryVm.Level > maxLevel)
            {
                AddToExpandedCategories(categoryVm);
            }
            else
            {
                var itemsToRemove = ExpandedCategories.Where(ci => ci.Level >= categoryVm.Level).ToList();
                itemsToRemove.ForEach(ci => ExpandedCategories.Remove(ci));
                AddToExpandedCategories(categoryVm);
            }
        }

        public CategoryViewModel SelectedTopCategory
        {
            get { return _selectedTopCategory; }
            set
            {
                Set(ref _selectedTopCategory, value);
                SelectedCategoryContent = value?.Content;
                SelectedCategoryHasChildren = value?.Children.Any() ?? false;
                ExpandedCategories.Clear();
                if (value != null)
                {
                    AddToExpandedCategories(value);
                }
            }
        }

        private void AddToExpandedCategories(CategoryViewModel categoryVm)
        {
            categoryVm.IsFolded = false;
            ExpandedCategories.Add(categoryVm);
            var prev = ExpandedCategories.FirstOrDefault(c => c.Level == categoryVm.Level - 1);
            if (prev != null)
            {
                prev.SelectedSubCategoryName = categoryVm.Name;
            }
        } 

        #endregion
    }
}