using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Media;
using DSA.Common.Utils;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Messages;
using DSA.Model.Models;
using DSA.Shell.ViewModels.Abstract;
using DSA.Shell.ViewModels.Builders;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using WinRTXamlToolkit.Tools;
using DSA.Shell.Controls.Media;
using System.Collections.Generic;


namespace DSA.Shell.ViewModels.Media
{
    public class MediaContentViewModel : DSAViewModelBase
    {
        // Attributes
        private readonly INavigationService _navigationService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly ISharingService _sharingService;
        private readonly IPresentationDataService _presentationDataService;
        private readonly IContentReviewDataService _contentReviewDataService;
        private readonly ICategoryContentDataService _categoryContentDataService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly ICategoryDataService _categoryDataService;
        

        private readonly PlayListCollection _playList = new PlayListCollection();
        private ObservableCollection<MediaPlaylistViewModel> _mediaPlaylists;
        private AddNewPlaylistViewModel _addNewPlaylistViewModel;
        private MediaLink _media;
        private string _name;

        private string _selectedPlayListId;
        private DateTime _startTime;
        private bool _contentEmailed;

        private ImageSource _mailIcon;
        private readonly ImageSource _emailEnableIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/MailIcons/mail@2x.png");
        private readonly ImageSource _emailDisableIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/MailIcons/mail_forbidden@2x.png");
        private readonly ImageSource _emailCheckedIcon = ImageUtil.GetImageSouce("ms-appx:///Assets/MailIcons/mail_marked@2x.png");

        private RelayCommand _navigateBackCommand;
        private RelayCommand _shareMediaCommand;
        private RelayCommand _openInExternalAppCommand;
        private RelayCommand _goToPreviousCommand;
        private RelayCommand _goToNextCommand;

        private bool _isPlaylistSelected;

        // *** Related Content ***
        private RelayCommand _showRelatedContentFlyoutCommand;
        private List<MediaLink> _relatedContent;
        private bool _relatedContentPresent;
        private RelatedContentFlyout _relatedContentFlyout;


        // CTOR
        public MediaContentViewModel(
             INavigationService navigationService,
             IPlaylistDataService playlistDataService,
             ISharingService sharingService,
             ISettingsDataService settingsDataService,
             IPresentationDataService presentationDataService,
             ICategoryContentDataService categoryContentDataService,
             IDocumentInfoDataService documentInfoDataService,
             ICategoryDataService categoryDataService,
             IContentReviewDataService contentReviewDataService) : base(settingsDataService)
        {
            _presentationDataService = presentationDataService;
            _navigationService = navigationService;
            _playlistDataService = playlistDataService;
            _sharingService = sharingService;
            _contentReviewDataService = contentReviewDataService;
            _categoryContentDataService = categoryContentDataService;
            _documentInfoDataService = documentInfoDataService;
            _categoryDataService = categoryDataService;


        // Create an empty list to work from
        _relatedContent = new List<MediaLink>();

            // Create the flyout
            _relatedContentFlyout = new RelatedContentFlyout();

            // Setup the messages
            RegisterMessages();
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<MediaLink>(
                this,
                async media =>
                {
                    // Close flyout if open
                    _relatedContentFlyout.CloseFlyout();

                    // Set the media
                    await SetMedia(media);
                    _playList.Clear();
                    _selectedPlayListId = null;
                    IsPlaylistSelected = false;
                    RefreshCommands();
                    HandleNotSupportedMedia(Media);
                });

            Messenger.Default.Register<PlaylistSelectedMessage>(
               this,
               async message =>
               {
                   var playListMedia = await _playlistDataService.GetPlayListMedia(message.PlaylistID);
                   _selectedPlayListId = message.PlaylistID;
                   playListMedia.ForEach(m => m.IsInternalMode = IsInternalModeEnable);
                   _playList.Clear();
                   _playList.AddRange(playListMedia.Where(m => m.IsVisible));
                   IsPlaylistSelected = true;
                   RefreshCommands();
               });

            Messenger.Default.Register<SwipeMessage>(
                this,
                 swipeMessage =>
                {
                    if(_navigationService.CurrentPageKey != ViewModelLocator.MediaContentPageKey)
                    {
                        return;
                    }

                    if(swipeMessage is SwipeMessage.LeftSwipe)
                    {
                        GoToNextCommand.Execute(this);
                    }
                    else
                    {
                        GoToPreviousCommand.Execute(this);
                    }
                });
        }

        private async Task SetMedia(MediaLink media)
        {
            _startTime = DateTime.Now;

            // Handle setting the Email Icon
            _contentEmailed = _presentationDataService.IsPresentationStarted() && _presentationDataService.IsEmailMarked(media);
            MailIcon = GetMailIcon(media, _contentEmailed);

            // Set the media
            Media = media;

            // Evaluate related content
            await CreateRelatedContentList(media);

            // Determine whether to hide the Relate Content Icon or not
            RelatedContentPresent = (_relatedContent == null || _relatedContent.Count == 0) ? false: true;
            Messenger.Default.Send(new RelatedContentMessage(_relatedContent));

            // Process associated Playlists
            MediaPlaylists = await GetMediaPlaylists(media);
            AddNewPlaylistViewModel = new AddNewPlaylistViewModel(media, _playlistDataService, MediaPlaylists);
        }

        private ImageSource GetMailIcon(MediaLink media, bool contentEmailed)
            {
                if (media.IsShareable == false)
                {
                    return _emailDisableIcon;
                }

                return contentEmailed
                        ? _emailCheckedIcon
                        : _emailEnableIcon;
            }

        public ImageSource RelatedContentIcon
        {
            get { return ImageUtil.GetImageSouce("ms-appx:///Assets/related_content.png"); }
        }

        private async Task StopShowingMedia(MediaLink media)
        {
            if(media == null)
            {
                return;
            }

            var viewedSecond = (DateTime.Now - _startTime).TotalSeconds;

            // Clean up related content
            _relatedContent.Clear();
            Messenger.Default.Send(new RelatedContentMessage(_relatedContent));

            if (_presentationDataService.IsPresentationStarted())
            {
                var contentReview = new ContentReview
                {
                    ContentId = media.ID,
                    ContentTitle = media.Name,
                    PlaylistId = _selectedPlayListId,
                    ContactId = string.Empty,
                    DocumentEmailed = _contentEmailed,
                    Rating = 0,
                    TimeViewed = viewedSecond
                };
                _presentationDataService.AddContentReview(new ContentReviewInfo { MediaLink = media, ContentReview = contentReview });
            }
            else
            {
                await _contentReviewDataService.SaveCompleteReviewToSoup(_media, _contentEmailed, viewedSecond, _selectedPlayListId, string.Empty);
            }
            Media = null;
        }

        private void HandleNotSupportedMedia(MediaLink media)
        {
           if(media != null && media.IsSupported == false)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    OpenInExternalAppCommand.Execute(this);
                });
            }
        }

        private async Task<ObservableCollection<MediaPlaylistViewModel>> GetMediaPlaylists(MediaLink media)
        {
            var playlists = await _playlistDataService.GetUserPlaylists();
            return new ObservableCollection<MediaPlaylistViewModel>(MediaPlaylistViewModelBuilder.Create(media, playlists, _playlistDataService));
        }

        public ObservableCollection<MediaPlaylistViewModel> MediaPlaylists
        {
            get { return _mediaPlaylists; }
            set { Set(ref _mediaPlaylists, value); }
        }

        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        public MediaLink Media
        {
            get
            {
                return _media;
            }
            set
            {
                Set(ref _media, value);
                Name = _media?.Name;
                ShareMediaCommand.RaiseCanExecuteChanged();
            }
        }

        public AddNewPlaylistViewModel AddNewPlaylistViewModel
        {
            get { return _addNewPlaylistViewModel; }
            set { Set(ref _addNewPlaylistViewModel, value); }
        }

        public RelayCommand NavigateBackCommand
        {
            get
            {
                return _navigateBackCommand ?? (_navigateBackCommand = new RelayCommand(async () =>
                {
                    await StopShowingMedia(Media);
                    _navigationService.GoBack();
                }));
            }
        }

        public ImageSource MailIcon
        {
            get
            {
                return _mailIcon;
            }
            set
            {
                Set(ref _mailIcon, value);
            }
        }

        public RelayCommand ShareMediaCommand
        {
            get
            {
                return _shareMediaCommand ?? (_shareMediaCommand = new RelayCommand(
                    async () =>
                    {
                        if(_presentationDataService.IsPresentationStarted())
                        {
                            _contentEmailed = !_contentEmailed;
                            MailIcon = GetMailIcon(Media, _contentEmailed);
                        }
                        else
                        {
                            await _sharingService.ShareMedia(Media);
                            _contentEmailed = true;
                        }
                      
                    },
                    () =>
                    {
                        return Media != null && Media.IsShareable;
                    }));
            }
        }

        public RelayCommand OpenInExternalAppCommand
        {
            get
            {
                return _openInExternalAppCommand ?? (_openInExternalAppCommand = new RelayCommand(
                    async () =>
                    {
                        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Media.Source));
                        var success = await Launcher.LaunchFileAsync(file, new LauncherOptions { DisplayApplicationPicker = false });
                    }));
            }
        }

        public RelayCommand GoToPreviousCommand
        {
            get
            {
                return _goToPreviousCommand ?? (_goToPreviousCommand = new RelayCommand(
                    async () =>
                     {
                         await SwipeMedia(_playList.GetPrevious);
                     },
                     () =>
                     {
                         return _playList.GetPrevious(Media).IsSome;
                     }));
            }
        }

        public RelayCommand GoToNextCommand
        {
            get
            {
                return _goToNextCommand ?? (_goToNextCommand = new RelayCommand(
                    async () =>
                    {
                        await SwipeMedia(_playList.GetNext);
                    },
                    () =>
                    {
                        return _playList.GetNext(Media).IsSome;
                    }));
            }
        }

        public RelayCommand ShowRelatedContentFlyoutCommand
        {
            get
            {
                return _showRelatedContentFlyoutCommand ?? (_showRelatedContentFlyoutCommand = new RelayCommand(
                                        () =>
                                        {
                                            // Send message to Related Content Flyout View Model
                                            _relatedContentFlyout.OpenFlyout();
                                        }
                                        ));
            }
        }


        private async Task SwipeMedia(Func<MediaLink , Option<MediaLink>> getMedia)
        {
            var next = getMedia(Media);

            if (next.IsSome)
            {
                await StopShowingMedia(Media);
                await SetMedia(next.Value);
                HandleNotSupportedMedia(Media);
            }

            RefreshCommands();
        }

        void RefreshCommands()
        {
            GoToPreviousCommand.RaiseCanExecuteChanged();
            GoToNextCommand.RaiseCanExecuteChanged();
        }

        public bool IsPlaylistSelected
        {
            get
            {
                return _isPlaylistSelected;
            }
            set
            {
                Set(ref _isPlaylistSelected, value);
            }
        }

        // //////////////////////////////////////////////////////////
        // *** Related Content ***
        // //////////////////////////////////////////////////////////
        private async Task CreateRelatedContentList(MediaLink media)
        {
            // Clear object if not empty
            _relatedContent.Clear();

            // Get the current user's MAC
            var mobileAppConfigurationId = await SettingsDataService.GetCurrentMobileConfigurationID();
            var categories = await _categoryDataService.GetCategories(mobileAppConfigurationId);
            

            // Get the CatCon list based on MACs
            var categoryContents = await _categoryContentDataService.GetCategoryContent(mobileAppConfigurationId);
            if (categoryContents != null && categoryContents.Any())
            {
                // Filter for the Category List based on the input mediaLink objects Id
                var tempCategoryIdList = (categoryContents.Where(cc => cc.ContentId == media.ID)).Select(x => x.CategoryId).Distinct();

                // Ge thet Parent category Ids and merge
                var parentCategoryIdList = categories.Where(x => tempCategoryIdList.Contains(x.Id)).Select(x => x.ParentCategory).Distinct();
                var categoryIdList = tempCategoryIdList.Concat(parentCategoryIdList).Distinct();

                if (categoryIdList != null && categoryIdList.Any())
                {
                    // Filter the Content ID List based on the previous category List
                    var contentIds = categoryContents.Where(cc => categoryIdList.Contains(cc.CategoryId) && cc.ContentId != media.ID).Select(x => x.ContentId).Distinct();
                    if (contentIds != null && contentIds.Any())
                    {
                        // Generate the MediaLink List
                        var docInfoList = await _documentInfoDataService.GetContentDocumentsByID(contentIds);
                        if (docInfoList != null && docInfoList.Any())
                        {
                            _relatedContent = docInfoList.Select(d => new MediaLink(d)).ToList();
                        }
                    }
                }
            }
        }

        public bool RelatedContentPresent
        {
            get { return _relatedContentPresent; }
            set { Set(ref _relatedContentPresent, value); }
        }
    }
}