using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Common.Extensions;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Sfdc.Sync;

namespace DSA.Data.Services
{
    /// <summary>
    /// Spotlight Data Service
    /// - refactoring
    /// - clean
    /// </summary>
    public class SpotlightDataService : ISpotlightDataService
    {
        #region Fields and Properties

        private readonly ICategoryInfoDataService _categoryInfoDataService;
        private readonly IContentDocumentDataService _contentDocumentDataService;
        private readonly ISyncDataService _syncDataService;
        private readonly IContentDistributionDataService _contentDistributionDataService;
        private readonly ISettingsDataService _settingsDataService;
        private readonly INewCategoryContentDataService _newCategoryContentDataService;

        #endregion

        #region Constructor

        public SpotlightDataService(
          ICategoryInfoDataService categoryInfoDataService,
          ISyncDataService syncDataService,
          IContentDocumentDataService contentDocumentDataService,
          IContentDistributionDataService contentDistributionDataService,
          ISettingsDataService settingsDataService,
          INewCategoryContentDataService newCategoryContentDataService)
        {
            _contentDistributionDataService = contentDistributionDataService;
            _categoryInfoDataService = categoryInfoDataService;
            _syncDataService = syncDataService;
            _contentDocumentDataService = contentDocumentDataService;
            _settingsDataService = settingsDataService;
            _newCategoryContentDataService = newCategoryContentDataService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get data for Spotlight:
        /// new and updated + featured items
        /// </summary>
        public async Task<List<SpotlightItem>> GetSpotlightData()
        {
            var featured = await GetFeaturedData();
            var newAndUpdated = await GetNewAndUpdated();
            return featured.AppendList(newAndUpdated);
        }

        /// <summary>
        /// Get new and updated documents
        /// </summary>
        private async Task<List<SpotlightItem>> GetNewAndUpdated()
        {
            var mobileAppConfigurationId = await _settingsDataService.GetCurrentMobileConfigurationID();
            var categoriesInfo = await _categoryInfoDataService.GetCategoryInfos();
            var newCategoryContents = await _newCategoryContentDataService.GetCategoryContent(mobileAppConfigurationId);
            var newCategoriesInfo = categoriesInfo.Where(ci => newCategoryContents.Any(cc => cc.CategoryId == ci.Category.Id));

            var lastSyncID = await _syncDataService.GetSyncConfig(SuccessfulSyncState.SyncedObjectMetaType.ContentDocuments.ToString());
            var documentsSyncInfo = await _syncDataService.GetSyncConfigs(lastSyncID?.SyncId);
            var contentDistributions = await _contentDistributionDataService.GetAllContentDistributions();
            var contentDocuments = await _contentDocumentDataService.GetContentDocumentsByID(documentsSyncInfo.Select(si => si.TransactionItemType).Union(newCategoryContents.Select(ncc => ncc.ContentId)));

            var allDocumentsSyncInfo = await _syncDataService.GetSyncConfigs(contentDocuments.Select(cd => cd.Id));
            var documentsWithSyncVersion = allDocumentsSyncInfo.Join(contentDocuments, si => si.TransactionItemType, cd => cd.Id, (si, cd) => new { Document = cd, Sync = si });
            var documentsInfos = documentsWithSyncVersion.Select(dsc => new DocumentInfo(dsc.Document, dsc.Sync, contentDistributions.FirstOrDefault(cd => cd.ContentVersionId == dsc.Document.LatestPublishedVersionId)));

            return documentsInfos.Select(fd =>
            {
                var categoryInfo = newCategoriesInfo.FirstOrDefault(csi => csi.Documents.Any(d => d.Document.Id == fd.Document.Id));
                categoryInfo = categoryInfo ?? categoriesInfo.FirstOrDefault(csi => csi.Documents.Any(d => d.Document.Id == fd.Document.Id));
                return new SpotlightItem
                {
                    Name = fd.Document.Title,
                    Description = fd.Document.Description,
                    Location = categoryInfo != null ? GetLocation(categoryInfo, categoriesInfo) : string.Empty,
                    Media = new MediaLink(fd),
                    AddedOn = fd.Document.PublishedVersionLastModifiedDate,
                    Group = SpotlightGroup.NewAndUpdated,
                };
            }).OrderByDescending(d => d.AddedOn.GetValueOrDefault().DayOfYear).ThenBy(d => d.Name).ToList();
        }

        /// <summary>
        /// Get featured documents
        /// </summary>
        public async Task<List<SpotlightItem>> GetFeaturedData()
        {
            var categoriesInfo = await _categoryInfoDataService.GetCategoryInfos();

            var featuredDocuments = categoriesInfo.SelectMany(
                    c => c.Documents.Where(d => d.Document.IsFeatured == true)
                                    .Select(d => new { Category = c, Document = d }))
                                    .GroupBy(g => g.Document.Document.Id)
                                    .Select(grp => grp.First());

            return featuredDocuments.Select(fd => new SpotlightItem
            {
                Name = fd.Document.Document.Title,
                Description = fd.Document.Document.Description,
                Location = GetLocation(fd.Category, categoriesInfo),
                Media = new MediaLink(fd.Document),
                AddedOn = fd.Document.Document.PublishedVersionLastModifiedDate,
                Group = SpotlightGroup.Featured,
            }).OrderByDescending(f => f.AddedOn.GetValueOrDefault().DayOfYear).ThenBy(d => d.Name).ToList();
        }

        /// <summary>
        /// Get document's location
        /// </summary>
        /// <param name="category"></param>
        /// <param name="categoriesInfo"></param>
        private string GetLocation(CategoryInfo category, List<CategoryInfo> categoriesInfo)
        {
            var path = GetPath(new List<CategoryInfo> { category }, categoriesInfo).Select(c => c.Category.Name).ToList();
            path.Reverse();

            return string.Join("/", path);
        }

        private List<CategoryInfo> GetPath(List<CategoryInfo> currentPath, List<CategoryInfo> categoriesInfo)
        {
            var last = currentPath.Last();
            var parent = categoriesInfo.FirstOrDefault(c => c.Category.Id == last.Category.ParentCategory);
            if (parent == null)
            {
                return currentPath;
            }
            currentPath.Add(parent);
            return GetPath(currentPath, categoriesInfo);
        }

        #endregion
    }
}

