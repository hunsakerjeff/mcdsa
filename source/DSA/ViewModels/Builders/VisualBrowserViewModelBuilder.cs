using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media;
using DSA.Common.Utils;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Models;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.VisualBrowser;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar;

namespace DSA.Shell.ViewModels.Builders
{
    public class VisualBrowserViewModelBuilder
    {
        public static IEnumerable<MainButtonViewModel> CreateButtonsViewModel(
            MobileConfigurationDTO mobileConfiguration, 
            Action<string> showCategoryAction)
        {
            var mainImage = ImageUtil.GetImageSouce(mobileConfiguration.ButtonConfiguration.Image);
            var selectedImage = ImageUtil.GetImageSouce(mobileConfiguration.ButtonConfiguration.SelectedImage);
            return mobileConfiguration.Buttons.Select(b => CreateButtonViewModel(b, mobileConfiguration.ButtonConfiguration, mainImage, selectedImage, showCategoryAction));
        }        

        private static MainButtonViewModel CreateButtonViewModel(
            ButtonDTO button, 
            ButtonConfigurationDTO buttonConfiguration, 
            ImageSource mainImage, 
            ImageSource selectedImage, 
            Action<string> showCategoryAction)
        {
            return new MainButtonViewModel(button, buttonConfiguration, mainImage, selectedImage, showCategoryAction);
        }

        public static IEnumerable<MobileConfigurationsSelectionViewModel> CreateMobileConfigurationsSelectionViewModel(
                IEnumerable<MobileAppConfig> mobileConfigurations,
                ISettingsDataService settingsDataService,
                string currentConfigurationId, 
                Action deselectAction, 
                Action closePopupAction)
        {
            return mobileConfigurations
                        .Select(mc => new MobileConfigurationsSelectionViewModel(mc, mc.Id == currentConfigurationId, settingsDataService, deselectAction, closePopupAction))
                        .ToList();
        }

        internal static IEnumerable<CategoryViewModel> CreateCategoriesViewModel(
            IEnumerable<CategoryBrowserDTO> topCategories, 
            NavigateToMediaCommand navigateToMediaCommand,
            bool isInternalMode,
            Action<CategoryViewModel> selectAction)
        {
            var currentLevel = 1;
            return topCategories.Select(tc => new CategoryViewModel
            (
                currentLevel,
                GetCategoryContent(tc, navigateToMediaCommand, isInternalMode),
                GetSubCategories(tc.SubCategories, navigateToMediaCommand, isInternalMode, currentLevel + 1, selectAction),
                selectAction
            )
           ).ToList();
        }

        private static List<CategoryViewModel> GetSubCategories(
            IEnumerable<CategoryBrowserDTO> subCategories, 
            NavigateToMediaCommand navigateToMediaCommand,
            bool isInternalMode,
            int level,
            Action<CategoryViewModel> selectAction)
        {
            return subCategories.Select(sc => new CategoryViewModel
            (
                level,
                GetCategoryContent(sc, navigateToMediaCommand, isInternalMode),
                GetSubCategories(sc.SubCategories, navigateToMediaCommand, isInternalMode, level + 1, selectAction),
                selectAction
            )).ToList();
        }

        private static CategoryContentViewModel GetCategoryContent(
            CategoryDTO tc, 
            NavigateToMediaCommand navigateToMediaCommand,
            bool isInternalMode)
        {
            return new CategoryContentViewModel
            {
                Name = tc.Name,
                Header = tc.Header,
                Description = tc.Description,
                BackgroundColor = ColorUtil.FromString(tc.BackgroundColor),
                Opacity = Convert.ToDouble(tc.BackgroundOpacity) / 100.0,
                PortraitImage = ImageUtil.GetImageSouce(tc.PortraitImage),
                LandscapeImage = ImageUtil.GetImageSouce(tc.LandscapeImage),
                AllMedia = GetCategoryMedia(tc.Documents, ImageUtil.GetImageSouce(tc.MediaButtonImage), navigateToMediaCommand),
                NavigationAreaBackgroundColor = tc.NavigationAreaBackgroundColor,
                NavigationAreaImage = ImageUtil.GetImageSouce(tc.NavigationAreaImage),
                TextColor = tc.TextColor,
                HeaderTextColor = tc.HeaderTextColor,
                IsInternalMode = isInternalMode,
                Order = tc.Order
            };
        }

        private static IEnumerable<CategoryMediaViewModel> GetCategoryMedia(
            IEnumerable<DocumentInfo> documents, 
            ImageSource backgroundImage, 
            NavigateToMediaCommand navigateToMediaCommand)
        {
            return documents
                    .Select(d =>
                    new CategoryMediaViewModel(new MediaLink(d), backgroundImage, navigateToMediaCommand)
                    ).ToList();
        }

    }
}
