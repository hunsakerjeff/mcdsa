using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model.Dto;

namespace DSA.Data.Services
{
    public class CategoryInfoDataService : ICategoryInfoDataService
    {
        private readonly ICategoryMobileConfigDataService _categoryMobileConfigDataService;
        private readonly ICategoryDataService _categoryDataService;
        private readonly ICategoryContentDataService _categoryContentDataService;
        private readonly IDocumentInfoDataService _documentInfoDataService;
        private readonly ISettingsDataService _settingsDataService;

        public CategoryInfoDataService(
           ICategoryMobileConfigDataService categoryMobileConfigDataService,
           ICategoryDataService categoryDataService,
           ICategoryContentDataService categoryContentDataService,
           IDocumentInfoDataService documentInfoDataService,
           ISettingsDataService settingsDataService)
        {
            _categoryMobileConfigDataService = categoryMobileConfigDataService;
            _categoryDataService = categoryDataService;
            _categoryContentDataService = categoryContentDataService;
            _documentInfoDataService = documentInfoDataService;
            _settingsDataService = settingsDataService;
        }

        public async Task<List<CategoryInfo>> GetCategoryInfos()
        {
            var mobileAppConfigurationId = await _settingsDataService.GetCurrentMobileConfigurationID();
            var mobileConfigCategories = await _categoryMobileConfigDataService.GetCategoryMobileConfigs(mobileAppConfigurationId);

            var categories = await _categoryDataService.GetCategories(mobileAppConfigurationId);
            var categoriesContent = await _categoryContentDataService.GetCategoryContent(mobileAppConfigurationId);

            var contentsID = categoriesContent.Select(cc => cc.ContentId15);

            var content = await _documentInfoDataService.GetContentDocumentsById15(contentsID);

            var documentsList = categoriesContent
                                     .Join(content, cc => cc.ContentId15, cd => cd.Document.Id15, (cc, cd) => new { CategoryContent = cc, Document = cd });

            return categories
                        .Join(mobileConfigCategories, mcc => mcc.Id, c => c.CategoryId, (c, mcc) => new { Config = mcc, Category = c })
                        .GroupJoin(documentsList, mc => mc.Category.Id, d => d.CategoryContent.CategoryId, (mc, ds) => new CategoryInfo { Config = mc.Config, Category = mc.Category, Documents = ds.Select(d => d.Document).ToList()})
                        .ToList();
        }
    }
}
