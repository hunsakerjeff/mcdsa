using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Common.Extensions;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Models;

namespace DSA.Data.Services
{
    public class BrowserDataService : IBrowserDataService
    {
        private readonly ICategoryInfoDataService _categoryInfoDataService;

        public BrowserDataService(
            ICategoryInfoDataService categoryInfoDataService)
        {
            _categoryInfoDataService = categoryInfoDataService;
        }

        public async Task<List<BrowserData>> GetBrowserData()
        {
            var categoriesInfo = await _categoryInfoDataService.GetCategoryInfos();

            var topCategories = categoriesInfo
                                    .Where(c => c.Category.IsTopLevel.HasValue && c.Category.IsTopLevel.Value)
                                    .Select(c => new BrowserData()
                                    {
                                        ID = c.Category.Id,
                                        Order = c.Category.Order,
                                        Name = c.Category.Name,
                                        Type = BrowserDataType.Category,
                                        Children = GetChildren(c.Category.Id, categoriesInfo).AppendList(GetDocuments(c.Category, categoriesInfo))
                                    })
                                    .OrderBy(b => b.Order)
                                    .ThenBy(b => b.Name)
                                    .ToList();

            return topCategories;
        }

        private List<BrowserData> GetChildren(string parentId, List<CategoryInfo> categoryInfo)
        {
            return categoryInfo.Where(c => c.Category.ParentCategory == parentId)
                          .Select(c => new BrowserData()
                          {
                              Order = c.Category.Order,
                              Name = c.Category.Name,
                              Type = BrowserDataType.Category,
                              Children = GetChildren(c.Category.Id, categoryInfo).AppendList(GetDocuments(c.Category, categoryInfo))
                          })
                          .OrderBy(b => b.Order)
                          .ThenBy(b => b.Name)
                          .ToList();
        }

        private List<BrowserData> GetDocuments(Category category, List<CategoryInfo> categoryInfo)
        {
            return categoryInfo.Where(c => c.Category.Id == category.Id)
                  .SelectMany(ci => ci.Documents.Select(d =>
                      new BrowserData
                      {
                          ID = d.Document.Id,
                          Name = d.Document.Title,
                          Type = BrowserDataType.Media,
                          Media = new MediaLink(d)
                      }
                  ))
                  .ToList();
        }
    }
}
