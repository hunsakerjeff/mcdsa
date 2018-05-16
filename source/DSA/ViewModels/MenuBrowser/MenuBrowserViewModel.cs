using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSA.Common.Extensions;
using DSA.Data.Interfaces;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight.Views;
using WinRTXamlToolkit.Tools;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar;

namespace DSA.Shell.ViewModels.MenuBrowser
{
    public class MenuBrowserViewModel : DSAViewModelBase
    {
        private readonly IBrowserDataService _browserDataService;
        private readonly INavigationService _navigationService;
        private readonly IHistoryDataService _historyDataService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly ISearchContentDataService _searchContentDataService;

        private CategoryItem _mainCategory;
        private ObservableCollection<CategoryItem> _categories;

        private SearchControlViewModel _searchViewModel;

        public MenuBrowserViewModel(
            IBrowserDataService browserDataService,
            INavigationService navigationService,
            IHistoryDataService historyDataService,
            ISearchContentDataService searchContentDataService,
            IDocumentInfoDataService documentInfoDataService,
            ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _browserDataService = browserDataService;
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
                var navigationCommand = new NavigateToMediaCommand(_navigationService, _historyDataService);
                var data = await _browserDataService.GetBrowserData();
                MainCategory = MenuBrowserCategoryContentViewModelBuilder.Create(data, navigationCommand, IsInternalModeEnable, SelectCategory);
                Categories = new ObservableCollection<CategoryItem> { MainCategory };

                // Setup the Search model
                SearchViewModel = new SearchControlViewModel(_documentInfoDataService, _searchContentDataService, _navigationService, navigationCommand);
            }
            catch (Exception ex)
            {
                // Report error here
            }
        }

        public SearchControlViewModel SearchViewModel
        {
            get { return _searchViewModel; }
            set { Set(ref _searchViewModel, value); }
        }

        private void SelectCategory(CategoryItem item)
        {
            var maxLevel = Categories.Select(ci => ci.Level).Max();
            if(item.Level > maxLevel)
            {
                Categories.Add(item);
            }
            else
            {
                var itemsToRemove = Categories.Where(ci => ci.Level >= item.Level).ToList();
                itemsToRemove.ForEach(ci => ci.SelectedCategory = null);
                itemsToRemove.ForEach(ci => Categories.Remove(ci));
                Categories.Add(item);
            }
        }

        public ObservableCollection<CategoryItem> Categories
        {
            get { return _categories; }
            set { Set(ref _categories, value); }
        }

        protected override void OnInternalModeChanged(bool value)
        {
            if(MainCategory == null)
            {
                return;
            }
            var list = MainCategory.CategoryContent.ToList().AppendList(GetChildren(MainCategory));
            list.ForEach(c => c.IsInternalMode = value);

            MainCategory.SelectedCategory = null;
            Categories.Clear();
            Categories.Add(MainCategory);
        }

        private IEnumerable<CategoryContentItem> GetChildren(CategoryContentItem cci)
        {
            if(cci is MediaItem)
            {
                return Enumerable.Empty<CategoryContentItem>();
            }

            var category = cci as CategoryItem;

            return category
                .CategoryContent
                .ToList()
                .AppendList(category.CategoryContent.SelectMany(c => GetChildren(c)));
        }

        public CategoryItem MainCategory
        {
            get { return _mainCategory; }
            set { Set(ref _mainCategory, value); }
        }
    }
}