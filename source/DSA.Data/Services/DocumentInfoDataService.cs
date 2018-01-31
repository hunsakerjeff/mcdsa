using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using DSA.Model.Models;

namespace DSA.Data.Services
{
    public class DocumentInfoDataService : IDocumentInfoDataService
    {
        private readonly IContentDocumentDataService _contentDocumentDataService;
        private readonly ISyncDataService _syncDataService;
        private readonly IContentDistributionDataService _contentDistributionDataService;

        public DocumentInfoDataService(
          IContentDocumentDataService contentDocumentDataService,
          ISyncDataService syncDataService,
          IContentDistributionDataService contentDistributionDataService)
        {
            _contentDistributionDataService = contentDistributionDataService;
            _contentDocumentDataService = contentDocumentDataService;
            _syncDataService = syncDataService;
        }

        public async Task<List<DocumentInfo>> GetMyLibraryContentDocuments()
        {
            var documents = await _contentDocumentDataService.GetMyLibraryContentDocuments();
            return await GetDocumentsInfo(documents);
        }

        private async Task<List<DocumentInfo>> GetDocumentsInfo(List<ContentDocument> documents)
        {
            var docIds = documents.Select(d => d.Id);
            var syncInfo = await _syncDataService.GetSyncConfigs(docIds);
            var contentDistributions = await _contentDistributionDataService.GetAllContentDistributions();
            var documentWithSyncVersion = documents.Join(syncInfo, d => d.Id, s => s.TransactionItemType, (d, s) => new { Document = d, Sync = s }).ToList();
            return documentWithSyncVersion
                        .Select(ds => new DocumentInfo(ds.Document, ds.Sync, contentDistributions.FirstOrDefault(cd => cd.ContentVersionId == ds.Document.LatestPublishedVersionId)))
                        .ToList();
        }

        public async Task<List<DocumentInfo>> GetContentDocumentsById15(IEnumerable<string> contentID15s)
        {
            var documents = await _contentDocumentDataService.GetContentDocumentsByID15(contentID15s);
            return await GetDocumentsInfo(documents);
        }

        public async Task<List<DocumentInfo>> GetContentDocumentsByID(IEnumerable<string> contentIDs)
        {
            var documents = await _contentDocumentDataService.GetContentDocumentsByID(contentIDs);
            return await GetDocumentsInfo(documents);
        }

        public async Task<List<DocumentInfo>> GetAllDocuments()
        {
            var documents = await _contentDocumentDataService.GetAllContentDocuments();
            return await GetDocumentsInfo(documents);
        }
    }
}
