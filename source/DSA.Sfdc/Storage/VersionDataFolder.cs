using System;
using System.Threading.Tasks;
using Windows.Storage;
using DSA.Sfdc.Sync;
using DSA.Sfdc.Util;

namespace DSA.Sfdc.Storage
{
    public class VersionDataFolder
    {
        private static readonly Lazy<VersionDataFolder> Lazy = new Lazy<VersionDataFolder>(() => new VersionDataFolder());

        public static VersionDataFolder Instance => Lazy.Value;

        public string FolderName { get; set; } = "VersionData";

        private readonly StorageFolder _localFolder;

        private VersionDataFolder()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<StorageFolder> CreateOrGetFolderForVersionDataId(string documentId, long syncId)
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                throw new ArgumentNullException(nameof(documentId), "Parameter documentId is required.");
            }

            try
            {
                StorageFolder localFolder = await CreateOrGetFolderForVersionData();

                string desiredName = documentId;

                var vdFolder = await localFolder.CreateFolderAsync(desiredName,
                    CreationCollisionOption.OpenIfExists);

                return await vdFolder.CreateFolderAsync(syncId.ToString(), CreationCollisionOption.OpenIfExists);

            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot create folder", ex.InnerException);
            }
        }

        private async Task<StorageFolder> CreateOrGetFolderForVersionData()
        {

            var vdFolder =
                await _localFolder.CreateFolderAsync(FolderName,
                    CreationCollisionOption.OpenIfExists);

            return vdFolder;
        }

        /// <summary>
        /// Queries filesystem for existence of Attachments folder in app data folder
        /// </summary>
        /// <returns>StorageFolder or null (when not exists or file instead of folder)</returns>
        public async Task<StorageFolder> GetVersionDataFolder()
        {
            var item = await _localFolder.TryGetItemAsync(FolderName);
            return (item != null && item.IsOfType(StorageItemTypes.Folder)) ? item as StorageFolder : null;
        }

        /// <summary>
        /// Permanently deletes Attachments folder with content
        /// </summary>
        /// <returns>true if no Attachment remains after call, false for error</returns>
        public async Task DeleteWithContent()
        {
            var vdFolder = await GetVersionDataFolder();

            try
            {
                var deleteAsync = vdFolder?.DeleteAsync(StorageDeleteOption.PermanentDelete);

                if (deleteAsync != null)
                    await deleteAsync;
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot delete version data folder", ex);
            }
        }

        public async Task DeleteVersionDataIdWithContent(string documentId, string syncId)
        {
            var vdFolder = await _localFolder.TryGetItemAsync(FolderName) as StorageFolder;
            if (vdFolder == null)
                return;

            var docFolder = await vdFolder.TryGetItemAsync(documentId) as StorageFolder;
            if (docFolder == null)
                return;

            var contentFolder = await docFolder.TryGetItemAsync(syncId) as StorageFolder;
            if (contentFolder == null)
                return;

            try
            {
                await contentFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot delete version data folder", ex);
            }
        }

        public async Task DeleteOldVersionsWithContent()
        {
            var vdFolder = await GetVersionDataFolder();
            await FolderUtil.CleanFolderFromOldVersions(vdFolder);
        }
    }
}
