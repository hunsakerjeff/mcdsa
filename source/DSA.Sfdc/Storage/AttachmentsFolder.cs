using System;
using System.Threading.Tasks;
using Windows.Storage;
using DSA.Sfdc.Sync;
using DSA.Sfdc.Util;

namespace DSA.Sfdc.Storage
{
    public class AttachmentsFolder
    {
        private static readonly Lazy<AttachmentsFolder> Lazy = new Lazy<AttachmentsFolder>(() => new AttachmentsFolder());

        public static AttachmentsFolder Instance => Lazy.Value;

        public string FolderName { get; set; } = "Attachments";

        private readonly StorageFolder _localFolder;

        private AttachmentsFolder()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<StorageFolder> CreateOrGetFolderForAttachmentId(string attachmentId, long syncId)
        {
            if (string.IsNullOrWhiteSpace(attachmentId))
            {
                throw new ArgumentNullException(nameof(attachmentId),"Parameter attachmentId is required.");
            }

            try
            {
                StorageFolder localFolder = await CreateOrGetFolderForAttachments();

                var attachmentFoler =  await localFolder.CreateFolderAsync(attachmentId, CreationCollisionOption.OpenIfExists);
                return await attachmentFoler.CreateFolderAsync(syncId.ToString(), CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot create folder", ex.InnerException);
            }
        }
       
        private async Task<StorageFolder> CreateOrGetFolderForAttachments()
        {
            var attFolder =
                await _localFolder.CreateFolderAsync(FolderName,
                    CreationCollisionOption.OpenIfExists);

            return attFolder;
        }

        /// <summary>
        /// Queries filesystem for existence of Attachments folder in app data folder
        /// </summary>
        /// <returns>StorageFolder or null (when not exists or file instead of folder)</returns>
        public async Task<StorageFolder> GetAttachmentsFolder()
        {
            var item = await _localFolder.TryGetItemAsync(FolderName);
            return item != null && item.IsOfType(StorageItemTypes.Folder) ? item as StorageFolder : null;
        }

        /// <summary>
        /// Permanently deletes Attachments folder with content
        /// </summary>
        /// <returns>true if no Attachment remains after call, false for error</returns>
        public async Task DeleteWithContent()
        {
            var attFolder = await GetAttachmentsFolder();
            
            try
            {
                var deleteAsync = attFolder?.DeleteAsync(StorageDeleteOption.PermanentDelete);

                if (deleteAsync != null)
                    await deleteAsync;
            }
            catch (Exception ex)
            {
                throw new SyncException("Cannot delete Attachments folder", ex);
            }
        }

        public async Task DeleteOldVersionsWithContent()
        {
            var attFolder = await GetAttachmentsFolder();
            await FolderUtil.CleanFolderFromOldVersions(attFolder);
        }
    }
}
