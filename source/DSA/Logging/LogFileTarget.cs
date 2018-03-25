using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace DSA.Shell.Logging
{
    public class LogFileTarget : LogTarget
    {
        // //////////////////////////////////////////////////////////
        // Constants
        // //////////////////////////////////////////////////////////
        protected const string kLogFolderName = "Logs";
        protected const string kLogFilePrefix = "DsaLog";


        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        static StorageFolder _logFolder = null;
        readonly AsyncLock _lock = new AsyncLock();


        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public int RetainDays { get; set; }
        protected DateTime NextCleanupDate { get; set; }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public LogFileTarget()
        {
            RetainDays = 30;

            // Setup Storage Folder for logging
            Initialize();
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        protected Task Initialize()
        {
            return InitializeAsync();
        }

        protected async Task InitializeAsync()
        {
            // Validate state
            if (_logFolder != null)
            {
                await Task.FromResult(true);
            }

            // Create the folder Storage object
            var root = ApplicationData.Current.LocalFolder;
            _logFolder = await root.CreateFolderAsync(kLogFolderName, CreationCollisionOption.OpenIfExists);
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Internal Methods
        // //////////////////////////////////////////////////////////
        internal async Task ForceCleanupAsync()
        {
            // Local Variables
            var threshold = DateTime.UtcNow.AddDays(0 - RetainDays);
            var regex = GenerateFilenameRegex();

            // Call cleanup
            await DoCleanup(regex, threshold);
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Override Methods
        // //////////////////////////////////////////////////////////
        protected override async Task<bool> WriteLogInternal(LogEvent evt)
        {
            // Note:  Initialize should happen before adding the target.  Add target should fail if not initialized

            // TODO:  Do level filtering here




            // Validate Object state
            if (_logFolder == null)
            {
                return await Task.FromResult(false);
            }

            // Generate a file name
            var fileName = GenerateFilename(evt);
            var message = evt.Serialize();

            // Lock this down to limint write access to log file
            using (await _lock.LockAsync().ConfigureAwait(false))
            {
                // Check for cleanup (which occurs each time a new log file is created - once a day)
                bool exists = await DoesLogExist(fileName).ConfigureAwait(false);
                if (!exists)
                {
                    // Cleanup older files
                    await CheckCleanupAsync().ConfigureAwait(false);
                }

                // Get a stream writer to write to file
                using (var filewriter = await GetStreamForFile(fileName).ConfigureAwait(false))
                {
                    // Write to the file
                    try
                    {
                        await filewriter.WriteLineAsync(message);
                    }
                    catch
                    {
                        return await Task.FromResult(false);
                    }
                }
            }
            return await Task.FromResult(true);
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Protected Methods
        // //////////////////////////////////////////////////////////
        protected string GenerateFilename(LogEvent evt)
        {
            var builder = new StringBuilder();
            builder.Append("DsaLog");
            builder.Append("_");
            builder.Append(evt.TimeStamp.ToString("yyyyMMdd"));
            builder.Append(".log");

            return builder.ToString();
        }

        public Regex GenerateFilenameRegex()
        {
            var builder = new StringBuilder();
            builder.Append("^");
            builder.Append(kLogFilePrefix);
            builder.Append(@"\s*_\s*");
            builder.Append("[0-9]{8}");
            builder.Append(".log$");

            // go...
            var regex = new Regex(builder.ToString(), RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return regex;
        }

        protected async Task<StreamWriter> GetStreamForFile(string fileName)
        {
            // Create or open the file
            var file = await _logFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            var stream = await file.OpenStreamForWriteAsync();

            // Make sure we're at the end of the stream for appending
            stream.Seek(0, SeekOrigin.End);

            // Setup the stream writer
            StreamWriter sw = new StreamWriter(stream);
            sw.AutoFlush = true;
            return sw;
        }

        protected async Task<bool> DoesLogExist(string fileName)
        {
            var item = await _logFolder.TryGetItemAsync(fileName);
            return item != null;
        }

        protected async Task CheckCleanupAsync()
        {
            // check if cleanup is necessary
            var now = DateTime.UtcNow;
            if (now < NextCleanupDate || RetainDays < 1)
            {
                return;
            }

            // Local Varaibles
            var threshold = now.AddDays(0 - RetainDays);
            var regex = GenerateFilenameRegex();

            // Attempt to cleanup
            try
            {
                await DoCleanup(regex, threshold);
            }
            finally
            {
                // reset...
                NextCleanupDate = DateTime.UtcNow.AddHours(1);
            }
        }

        protected async Task DoCleanup(Regex pattern, DateTime threshold)
        {
            var toDelete = new List<StorageFile>();

            foreach (var file in await _logFolder.GetFilesAsync())
            {
                if (pattern.Match(file.Name).Success && file.DateCreated <= threshold)
                {
                    toDelete.Add(file);
                }
            }

            // walk the delete file List
            foreach (var file in toDelete)
            {
                try
                {
                    await file.DeleteAsync();
                }
                catch (Exception ex)
                {
                    //InternalLogger.Current.Warn($"Failed to delete '{file.Path}'.", ex);
                }
            }
        }
    }
}

