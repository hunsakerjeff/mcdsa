using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model.Dto;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.MenuBrowser;

namespace DSA.Shell.ViewModels.Builders
{
    public class MenuBrowserCategoryContentViewModelBuilder
    {
        internal static CategoryItem Create(List<BrowserData> modelData, NavigateToMediaCommand navigationCommand, bool isInternalModeEnable, Action<CategoryItem> selectCategoryAction)
        {
            Func<BrowserData, CategoryContentItem> createfunction = (md) => CreteCategoryItem(md, navigationCommand, 1, isInternalModeEnable, selectCategoryAction);
            var items = modelData.Select(createfunction);
            return new CategoryItem(0, string.Empty, "Categories", isInternalModeEnable, selectCategoryAction, items.ToList());
        }

        private static CategoryContentItem CreteCategoryItem(BrowserData modelData, NavigateToMediaCommand navigationCommand, int level, bool isInternalModeEnable, Action<CategoryItem> selectCategoryAction)
        {
            if(modelData.Type == BrowserDataType.Media)
            {
                return new MediaItem(modelData.Media, navigationCommand, isInternalModeEnable);
            }
            else
            {
                var children = modelData.Children.OrderBy(x=>x.Order).ThenBy(x=>x.Name).Select(md => CreteCategoryItem(md, navigationCommand, level + 1, isInternalModeEnable, selectCategoryAction));
                return new CategoryItem(level, modelData.ID, modelData.Name, isInternalModeEnable, selectCategoryAction, children.ToList());
            }
        }
    }
}
