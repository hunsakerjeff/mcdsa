using System;
using System.Threading.Tasks;
using Windows.Storage;
using DSA.Sfdc.Sync;

namespace DSA.Sfdc.Storage
{
    public class ContentThumbnailFolder
    {
        private static readonly Lazy<ContentThumbnailFolder> Lazy = new Lazy<ContentThumbnailFolder>(() => new ContentThumbnailFolder());

        public static ContentThumbnailFolder Instance => Lazy.Value;

        public string FolderName { get; set; } = "ContentThumbnails";

        private readonly StorageFolder _localFolder;

        private ContentThumbnailFolder()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<StorageFolder> CreateOrGetFolderForContentThumbnailId(string contentThumbnailId)
        {
            if (string.IsNullOrWhiteSpace(contentThumbnailId))
            {
                throw new ArgumentNullException("Parameter content ThumbnailId is required.");
            }

            try
            {
                StorageFolder localFolder = await CreateOrGetFolderForContentThumbnail();

                string desiredName = contentThumbnailId;

                return await localFolder.CreateFolderAsync(desiredName, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot create folder", ex.InnerException);
            }
        }

        private async Task<StorageFolder> CreateOrGetFolderForContentThumbnail()
        {

            var ctFolder =
                await _localFolder.CreateFolderAsync(FolderName,
                    CreationCollisionOption.OpenIfExists);

            return ctFolder;
        }

        /// <summary>
        /// Queries filesystem for existence of Content Thumbnail folder in app data folder
        /// </summary>
        /// <returns>StorageFolder or null (when not exists or file instead of folder)</returns>
        public async Task<StorageFolder> GetContentThumbnailFolder()
        {
            var item = await _localFolder.TryGetItemAsync(FolderName);
            return (item != null && item.IsOfType(StorageItemTypes.Folder)) ? item as StorageFolder : null;
        }

        /// <summary>
        /// Permanently deletes Content Thumbnail folder with content
        /// </summary>
        /// <returns>true if no Content Thumbnail remains after call, false for error</returns>
        public async Task DeleteWithContent()
        {
            var ctFolder = await GetContentThumbnailFolder();

            try
            {
                var deleteAsync = ctFolder?.DeleteAsync(StorageDeleteOption.PermanentDelete);

                if (deleteAsync != null)
                    await deleteAsync;
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot delete content thumbnail folder", ex);
            }
        }

        public async Task DeleteContentThumbnailIdWithContent(string contentThumbnailId)
        {
            var parentFolder = await _localFolder.TryGetItemAsync(FolderName) as StorageFolder;
            if (parentFolder == null)
                return;

            var ctFolder = await parentFolder.TryGetItemAsync(contentThumbnailId) as StorageFolder;
            if (ctFolder == null)
                return;

            try
            {
                await ctFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot delete content thumbnail folder", ex);
            }
        }
    }
}

