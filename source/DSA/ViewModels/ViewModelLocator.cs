using System.Diagnostics.CodeAnalysis;
using DSA.Data.Interfaces;
using DSA.Data.Services;
using DSA.Data.Services.API;
using DSA.Model;
using DSA.Shell.Pages;
using DSA.Shell.ViewModels.Common;
using DSA.Shell.ViewModels.History;
using DSA.Shell.ViewModels.Media;
using DSA.Shell.ViewModels.MenuBrowser;
using DSA.Shell.ViewModels.Playlist;
using DSA.Shell.ViewModels.Search;
using DSA.Shell.ViewModels.Spotlight;
using DSA.Shell.ViewModels.VisualBrowser;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using Microsoft.Practices.ServiceLocation;
using HistoryPage = DSA.Shell.Pages.HistoryPage;
using MediaContentPage = DSA.Shell.Pages.MediaContentPage;
using MenuBrowserPage = DSA.Shell.Pages.MenuBrowserPage;
using PlaylistsPage = DSA.Shell.Pages.PlaylistsPage;
using SearchPage = DSA.Shell.Pages.SearchPage;
using SpotlightPage = DSA.Shell.Pages.SpotlightPage;


namespace DSA.Shell.ViewModels
{
    public class ViewModelLocator
    {
        public const string VisualBrowserPageKey = "VisualBrowserPage";
        public const string MenuBrowserPageKey = "MenuBrowserPage";
        public const string HistoryPageKey = "HistoryPage";
        public const string PlaylistPageKey = "PlaylistsPage";
        public const string SpotlightPageKey = "SpotlightPage";
        public const string MediaContentPageKey = "MediaContentPage";
        public const string SearchPageKey = "SearchPageKey";

        [SuppressMessage("Microsoft.Performance",
           "CA1822:MarkMembersAsStatic",
           Justification = "This non-static member is needed for data binding purposes.")]
        public VisualBrowserViewModel VisualBrowser
        {
            get
            {
                return ServiceLocator.Current.GetInstance<VisualBrowserViewModel>();
            }
        }


        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MainAppBarViewModel AppBar
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainAppBarViewModel>();
            }
        }

        [SuppressMessage("Microsoft.Performance",
           "CA1822:MarkMembersAsStatic",
           Justification = "This non-static member is needed for data binding purposes.")]
        public NewContentBarViewModel NewContentBar
        {
            get
            {
                return ServiceLocator.Current.GetInstance<NewContentBarViewModel>();
            }
        }
        

        [SuppressMessage("Microsoft.Performance",
           "CA1822:MarkMembersAsStatic",
           Justification = "This non-static member is needed for data binding purposes.")]
        public MenuBrowserViewModel MenuBrowser
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MenuBrowserViewModel>();
            }
        }

        [SuppressMessage("Microsoft.Performance",
          "CA1822:MarkMembersAsStatic",
          Justification = "This non-static member is needed for data binding purposes.")]
        public HistoryViewModel History
        {
            get
            {
                return ServiceLocator.Current.GetInstance<HistoryViewModel>();
            }
        }

        [SuppressMessage("Microsoft.Performance",
         "CA1822:MarkMembersAsStatic",
         Justification = "This non-static member is needed for data binding purposes.")]
        public SpotlightViewModel Spotlight
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SpotlightViewModel>();
            }
        }

        [SuppressMessage("Microsoft.Performance",
        "CA1822:MarkMembersAsStatic",
        Justification = "This non-static member is needed for data binding purposes.")]
        public MediaContentViewModel MediaContent
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MediaContentViewModel>();
            }
        }

        [SuppressMessage("Microsoft.Performance",
       "CA1822:MarkMembersAsStatic",
       Justification = "This non-static member is needed for data binding purposes.")]
        public PlaylistsViewModel Playlists
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PlaylistsViewModel>();
            }
        }

        [SuppressMessage("Microsoft.Performance",
        "CA1822:MarkMembersAsStatic",
        Justification = "This non-static member is needed for data binding purposes.")]
        public SearchPageViewModel SearchPage
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SearchPageViewModel>();
            }
        }


        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            // 
            SimpleIoc.Default.Register<SfdcConfig>();
            RegisterDataServices();

            var nav = GetNavigationService();
            SimpleIoc.Default.Register<INavigationService>(() => nav);

            SimpleIoc.Default.Register<IDialogService, DialogService>();

            SimpleIoc.Default.Register<INetworkInformationService, NetworkInformationService>(createInstanceImmediately: true);

            RegisterViewModels();
        }

        private static void RegisterViewModels()
        {
            SimpleIoc.Default.Register<MainAppBarViewModel>();
            SimpleIoc.Default.Register<MenuBrowserViewModel>();
            SimpleIoc.Default.Register<HistoryViewModel>();
            SimpleIoc.Default.Register<SpotlightViewModel>();
            SimpleIoc.Default.Register<PlaylistsViewModel>();
            SimpleIoc.Default.Register<VisualBrowserViewModel>();
            SimpleIoc.Default.Register<MediaContentViewModel>(true);
            SimpleIoc.Default.Register<NewContentBarViewModel>(true);
            SimpleIoc.Default.Register<SearchPageViewModel>(true);
        }

        private static void RegisterDataServices()
        {
            SimpleIoc.Default.Register<IMobileConfigurationDataService, MobileConfigurationDataService>();
            SimpleIoc.Default.Register<IBrowserDataService, BrowserDataService>();
            SimpleIoc.Default.Register<IHistoryDataService, HistoryDataService>();
            SimpleIoc.Default.Register<ISpotlightDataService, SpotlightDataService>();
            SimpleIoc.Default.Register<IPlaylistDataService, PlaylistDataService>();
            
            //API
            SimpleIoc.Default.Register<IDocumentInfoDataService, DocumentInfoDataService>();
            SimpleIoc.Default.Register<IAttachmentDataService, AttachmentDataService>();
            SimpleIoc.Default.Register<IContentDocumentDataService, ContentDocumentDataService>();
            SimpleIoc.Default.Register<ICategoryContentDataService, CategoryContentDataService>();
            SimpleIoc.Default.Register<INewCategoryContentDataService, NewCategoryContentDataService>();
            SimpleIoc.Default.Register<IMobileAppConfigDataService, MobileAppConfigDataService>();
            SimpleIoc.Default.Register<ICategoryMobileConfigDataService, CategoryMobileConfigDataService>();
            SimpleIoc.Default.Register<ICategoryDataService, CategoryDataService>();
            SimpleIoc.Default.Register<IFeaturedPlaylistDataService, FeaturedPlaylistDataService>();
            SimpleIoc.Default.Register<ISyncDataService, SyncDataService>();
            SimpleIoc.Default.Register<IUserSettingsDataService, UserSettingsDataService>();
            SimpleIoc.Default.Register<IContactsService, ContactsService>();
            SimpleIoc.Default.Register<IContentDistributionDataService, ContentDistributionDataService>();
            
            SimpleIoc.Default.Register<IEventDataService, EventDataService>();
            SimpleIoc.Default.Register<IContentReviewDataService, ContentReviewDataService>();
            SimpleIoc.Default.Register<IUserSessionService, UserSessionService>();
            SimpleIoc.Default.Register<IPresentationDataService, PresentationDataService>();
            SimpleIoc.Default.Register<ISearchContentDataService, SearchContentDataService>();
            SimpleIoc.Default.Register<ISyncLogService, SyncLogService>();
            SimpleIoc.Default.Register<ISearchTermDataService, SearchTermDataService>();

            SimpleIoc.Default.Register<ICategoryInfoDataService, CategoryInfoDataService>();
            SimpleIoc.Default.Register<ISettingsDataService, SettingsDataService>();
            SimpleIoc.Default.Register<IFileService, FileService>();
            SimpleIoc.Default.Register<IGeolocationService, GeolocationService>();
            SimpleIoc.Default.Register<ISharingService, SharingService>(createInstanceImmediately: true);

        }

        public static void AttachSharingService()
        {
            SimpleIoc.Default.GetInstance<ISharingService>().Attach();
        }

        private static NavigationService GetNavigationService()
        {
            var nav = new NavigationService();

            nav.Configure(VisualBrowserPageKey, typeof(VisualBrowserPage));
            nav.Configure(MenuBrowserPageKey, typeof(MenuBrowserPage));
            nav.Configure(HistoryPageKey, typeof(HistoryPage));
            nav.Configure(PlaylistPageKey, typeof(PlaylistsPage));
            nav.Configure(SpotlightPageKey, typeof(SpotlightPage));
            nav.Configure(MediaContentPageKey, typeof(MediaContentPage));
            nav.Configure(SearchPageKey, typeof(SearchPage));
            return nav;
        }

        public static void Cleanup()
        {
        }
    }
}