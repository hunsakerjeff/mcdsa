using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DSA.Common.Extensions;
using DSA.Common.Utils;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Models;

namespace DSA.Data.Services
{
    /// <summary>
    /// Mobile Configuration DataService
    /// - clean
    /// </summary>
    public class MobileConfigurationDataService : IMobileConfigurationDataService
    {
        private readonly IMobileAppConfigDataService _mobileAppConfigDataService;
        private readonly ISettingsDataService _settingsDataService;
        private readonly IAttachmentDataService _attachmentDataService;
        private readonly ICategoryInfoDataService _categoryInfoDataService;
        private readonly ISyncDataService _syncDataService;

        public MobileConfigurationDataService(
            ICategoryInfoDataService categoryInfoDataService,
            ISettingsDataService settingsDataService,
            IMobileAppConfigDataService mobileAppConfigDataService,
            IAttachmentDataService mobileAppConfigAttachmentDataService,
            ISyncDataService syncDataService)
        {
            _syncDataService = syncDataService;
            _categoryInfoDataService = categoryInfoDataService;
            _settingsDataService = settingsDataService;
            _attachmentDataService = mobileAppConfigAttachmentDataService;
            _mobileAppConfigDataService = mobileAppConfigDataService;
        }

        public async Task<MobileConfigurationDTO> GetCurrentMobileConfiguration()
        {
            var currentMCID = await _settingsDataService.GetCurrentMobileConfigurationID();
            var mobileApp = await _mobileAppConfigDataService.GetMobileAppConfig(currentMCID);
            if (mobileApp == null)
            {
                return GetDefaultMobileConfiguration();
            }

            var categoryInfos = await _categoryInfoDataService.GetCategoryInfos();
            var topCategories = categoryInfos.Where(ci => ci.Category.IsTopLevel.HasValue && ci.Category.IsTopLevel.Value);

            var attachments = await GetAttachments(mobileApp, categoryInfos);
            var syncInfo = await _syncDataService.GetSyncConfigs(attachments.Select(at => at.Id));

            var attSyncDictionary = attachments.Join(syncInfo, at => at.Id, si => si.TransactionItemType, (at, si) => new { AttachementId = at.Id, SyncId = si.SyncId }).ToDictionary(i => i.AttachementId, i => i.SyncId);
            Func<string, string> getSyncId = id =>
            {
                return string.IsNullOrWhiteSpace(id) == false && attSyncDictionary.Keys.Contains(id)
                    ? attSyncDictionary[id]
                    : string.Empty;
            };
            Func<string, string> getImage = id =>
            {
                var att = attachments.FirstOrDefault(a => a.Id == id);
                return att == null ? string.Empty : ImageUtil.GetPathFromAttachment(att.Id, getSyncId(id), att.Name);
            };

            return new MobileConfigurationDTO()
            {
                Name = mobileApp.Title,
                LandscapeBackgroundImage = getImage(mobileApp.LandscapeAttachmentId),
                PortraitBackgroundImage = getImage(mobileApp.PortraitAttachmentId),
                LogoImage = getImage(mobileApp.LogoAttachmentId),
                ButtonConfiguration = GetButtonConfiguration(mobileApp, getImage),
                Buttons = topCategories.Select(ci => new ButtonDTO(ci)).ToList(),
                TopCategories = GetCategories(topCategories, categoryInfos, getImage),
                ReportProblemEmail = mobileApp.ReportAnIssue
            };
        }

        private MobileConfigurationDTO GetDefaultMobileConfiguration()
        {
            return new MobileConfigurationDTO()
            {
                Name = string.Empty,
                LandscapeBackgroundImage = "ms-appx:///Assets/Default-Landscape~ipad@2x.png",
                PortraitBackgroundImage = "ms-appx:///Assets/Default-Portrait~ipad@2x.png",
                LogoImage = "ms-appx:///Assets/SalesforceServices@2x.png",
                ButtonConfiguration = new ButtonConfigurationDTO(),
                Buttons = Enumerable.Empty<ButtonDTO>(),
                TopCategories = Enumerable.Empty<CategoryBrowserDTO>()
            };
        }

        private async Task<List<AttachmentMetadata>> GetAttachments(MobileAppConfig mobileApp, List<CategoryInfo> categoryInfos)
        {
            var categoriesAttachments = categoryInfos
                                            .SelectMany(ci => new[]
                                                {
                                                    ci.Category.ThumbnailAttachmentId,
                                                    ci.Config.LandscapeAttachmentId,
                                                    ci.Config.PortraitAttachmentId,
                                                    ci.Config.ContentAttachmentId,
                                                    ci.Config.ContentOverAttachmentId,
                                                    ci.Category.GalleryAttachmentId})
                                           .ToList();

            var attachmentsIDs = mobileApp
                                    .GetAllAttachmentIds()
                                    .ToList()
                                    .AppendList(categoriesAttachments)
                                    .Distinct()
                                    .Where(id => string.IsNullOrWhiteSpace(id) == false);

            return await _attachmentDataService.GetAttachmentsMetadata(attachmentsIDs);
        }

        private IEnumerable<CategoryBrowserDTO> GetCategories(IEnumerable<CategoryInfo> topCategories, List<CategoryInfo> categoryInfos, Func<string, string> getCategoriesImage)
        {
            return topCategories.Select(tci =>
            {
                return FillProperties(new CategoryBrowserDTO(), tci, categoryInfos, getCategoriesImage);
            })
            .OrderBy(b => b.Order)
            .ThenBy(b => b.Name)
            .ToList();
        }

        private IEnumerable<CategoryBrowserDTO> GetSubCategories(CategoryInfo tci, List<CategoryInfo> categoryInfos, Func<string, string> getCategoriesImage)
        {
            return categoryInfos
                     .Where(ci => ci.Category.ParentCategory == tci.Category.Id)
                     .Select(sc => FillProperties(new CategoryBrowserDTO(), sc, categoryInfos, getCategoriesImage))
                     .OrderBy(b => b.Order)
                     .ThenBy(b => b.Name)
                     .ToList();
        }

        private CategoryBrowserDTO FillProperties(CategoryBrowserDTO dto, CategoryInfo ci, List<CategoryInfo> categoryInfos, Func<string, string> getImage)
        {
            dto.Name = ci.Category.Name;
            dto.Header = ci.Config.GalleryHeadingText;
            dto.Description = ci.Category.Description;
            dto.TextColor = ColorUtil.FromString(ci.Config.OverlayTextColor);
            dto.HeaderTextColor = ColorUtil.FromString(ci.Config.GalleryHeadingTextColor);
            dto.LandscapeImage = getImage(ci.Config.LandscapeAttachmentId);
            dto.PortraitImage = getImage(ci.Config.PortraitAttachmentId);
            dto.BackgroundColor = ci.Config.OverlayBgColor;
            dto.BackgroundOpacity = ci.Config.OverlayBgAlpha.HasValue ? ci.Config.OverlayBgAlpha.Value : 0;
            dto.MediaButtonImage = getImage(ci.Config.ContentAttachmentId);
            dto.SelectedMediaButtonImage = getImage(ci.Config.ContentOverAttachmentId);
            dto.NavigationAreaBackgroundColor = ColorUtil.FromString(ci.Config.SubCategoryBackgroundColor);
            dto.NavigationAreaImage = getImage(ci.Category.ThumbnailAttachmentId);
            dto.Documents = ci.Documents;
            dto.SubCategories = GetSubCategories(ci, categoryInfos, getImage);
            dto.Order = ci.Category.Order;
            return dto;
        }

        private static ButtonConfigurationDTO GetButtonConfiguration(MobileAppConfig mobileApp, Func<string, string> getImage)
        {
            int opacityInt;
            var opacity = int.TryParse(mobileApp.ButtonTextAlpha, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out opacityInt)
                            ? opacityInt / 100.0
                            : 1.0;

            return new ButtonConfigurationDTO()
            {
                Image = getImage(mobileApp.ButtonDefaultAttachmentId),
                SelectedImage = getImage(mobileApp.ButtonHighlightAttachmentId),
                TextColor = ColorUtil.FromString(mobileApp.ButtonTextColor),
                Opacity = opacity,
                HighlightTextColor = ColorUtil.FromString(mobileApp.ButtonHighlightTextColor)
            };
        }

        public async Task<ImageSource> GetLogoImage()
        {
            var currentMC = await GetCurrentMobileConfiguration();
            return string.IsNullOrWhiteSpace(currentMC.LogoImage) == false
                    ? new BitmapImage(new Uri(currentMC.LogoImage)) as ImageSource
                    : null;
        }

        //public async Task<string> GetCurrentMobileConfigurationName()
        //{
        //    var currentMCID = await _settingsDataService.GetCurrentMobileConfigurationID();
        //    var mc = await _mobileAppConfigDataService.GetMobileAppConfig(currentMCID);
        //    return mc?.Title;
        //}
    }
}
