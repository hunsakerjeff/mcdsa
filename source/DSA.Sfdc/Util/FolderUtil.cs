using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using DSA.Sfdc.Sync;

namespace DSA.Sfdc.Util
{
    public static class FolderUtil
    {
        public static async Task CleanFolderFromOldVersions(StorageFolder storageFolder)
        {
            if (storageFolder != null)
            {
                var foldersLocal = SuccessfulSyncState.Instance.GetAllSuccessfulSyncsFromSoup().Select(ss => ss.TransactionItemType).ToList();
                var folders = await storageFolder.GetFoldersAsync();
                var foldersToDelete = folders.Where(ff => !foldersLocal.Contains(ff.Name));
                var foldersToClean = folders.Where(ff => foldersLocal.Contains(ff.Name));
                foreach (var folder in foldersToDelete)
                {
                    try
                    {
                        await folder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine($"Unable to delete { storageFolder.Name } : { folder.Name }");
                    }
                }
                foreach (var folder in foldersToClean)
                {
                    var versionFolders = (await folder.GetFoldersAsync()).OrderBy(ff => ff.DateCreated).ToList();
                    var count = versionFolders.Count();
                    for (var i = 0; i < count; i++)
                    {
                        if (i != count - 1)
                        {
                            try
                            {
                                await versionFolders[i].DeleteAsync(StorageDeleteOption.PermanentDelete);
                            }
                            catch (Exception)
                            {
                                Debug.WriteLine($"Unable to delete { storageFolder.Name } : { folder.Name }");
                            }
                        }
                    }
                }
            }
        }
    }
}
