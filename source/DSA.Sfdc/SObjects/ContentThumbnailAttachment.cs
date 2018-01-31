using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using DSA.Sfdc.Storage;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class ContentThumbnailAttachment : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("ParentId", SmartStoreType.SmartString)
        };

        internal override string SoqlQuery
        {
            get
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("ContentDocument", "Id", QuerySpec.SqlOrder.ASC, PageSize);

                var ids = globalStore.Query(querySpec, 0).Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.ContentDocument>(item.ToString()).ContentThumbnailId)
                    .ToList();
                if (!ids.Any())
                {
                    Debug.WriteLine("RETURNING NULL");
                    return null;
                }

                var query = "SELECT ContentType," +
                                "Id," +
                                "IsPrivate," +
                                "LastModifiedDate," +
                                "Name, " +
                                "ParentId, " +
                                "BodyLength " +
                            "FROM Attachment " +
                            "WHERE ParentId IN " +
                            "('" + ids.Aggregate((i, j) => i + "','" + j) + "')";
                return query;
            }
        }

        internal ContentThumbnailAttachment(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);

            SoupName = "AttachmentMetadata";
            TempSoupName = "ContentThumbnailAttMeta";
        }

        internal IList<AttachmentMetadata> GetContentThumbnailAttachmentsFromSoup()
        {
            var querySpec = QuerySpec.BuildAllQuerySpec(TempSoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize);
            var results = Store.Query(querySpec, 0);

            var macList = results.Select(item => CustomPrefixJsonConvert.DeserializeObject<AttachmentMetadata>(item.ToString())).ToList();

            var endWithError = SaveMetadataToSoup(results);
            if (Store.HasSoup(TempSoupName) && !endWithError)
            {
                Store.DropSoup(TempSoupName);
            }

            CleanUpContentThumbnails(results);

            return macList;
        }

        private async void CleanUpContentThumbnails(JArray results)
        {
            var parentIds = results.Select(item => CustomPrefixJsonConvert.DeserializeObject<AttachmentMetadata>(item.ToString()).ParentId).ToList();
            var ctFolder = await ContentThumbnailFolder.Instance.GetContentThumbnailFolder();
            if (ctFolder != null)
            {
                var thumbnailFolders = await ctFolder.GetFoldersAsync();
                var idsToDelete = new List<long>();
                var globalStore = SmartStore.GetGlobalSmartStore();
                foreach (var thumbnailFolder in thumbnailFolders)
                {
                    if (!parentIds.Contains(thumbnailFolder.Name))
                    {
                        Debug.WriteLine("Delete Content Thumbnail " + thumbnailFolder.Name);
                        thumbnailFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        idsToDelete.Add(globalStore.LookupSoupEntryId(SoupName, "ParentId", thumbnailFolder.Name));
                    }
                }
                if (idsToDelete.Any())
                {
                    Store.Delete(SoupName, idsToDelete.ToArray(), false);
                }
            }
        }

    }
}