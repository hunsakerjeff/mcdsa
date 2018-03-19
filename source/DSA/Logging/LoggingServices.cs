using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace DSA.Shell.Logging
{
    public class LoggingServices
    {
        #region Attributes
        private static readonly LoggingServices instance = new LoggingServices();

        // We would like a Config, target, logger
        //private string _logFullPath;
        private const string kSuffix = "Logs";
        private const string kLogName = "DSA.log";
        private static StorageFolder _logFolder;
        readonly AsyncLock _lock = new AsyncLock();

        #endregion

        #region Properties
        // Properties
        public bool Enabled { get; set; } = true;
        public LoggingLevel LogLevel { get; set; } = LoggingLevel.Information;
        public int RetainDays { get; set; } = 10;
        public bool Initialized { get; protected set; }

        #endregion

        #region CTOR
        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static LoggingServices()
        {
        }

        public static LoggingServices Instance
        {
            get { return instance; }
        }
        #endregion

        #region Implementation - Public functions

        public async void Initialize()
        {
            // Setup Log folder
            if (_logFolder == null)
            {
                var root = ApplicationData.Current.LocalFolder;
                _logFolder = await root.CreateFolderAsync(kSuffix, CreationCollisionOption.OpenIfExists);
                Initialized = (null == _logFolder) ? false: true;
            }

            Initialized = true;
        }

        public async void LogMessage(object obj, LoggingLevel level)
        {
            // Validate state
            if (!Initialized)
            {
                Initialize();
            }

            // Check logging enabled
            if (!Enabled)
            {
                return;
            }

            // Check message Level
            if (level < LogLevel)
            {
                return;
            }

            // Check and handle types of log message
            var msg = obj as string;
            var ex = obj as Exception;
            LogEvent evt = null;
            string logger = "DSALog";

            // Create a logEvent Object
            if (msg != null)            // String option    
            {
                // Create a LogEvent Object
                evt = new LogEvent(level, logger, msg, null);
            }
            else if (ex != null)        // Exception Option
            {
                evt = new LogEvent(level, logger, "Exception: ", ex);
            }
            else                        // Undefined Option
            {
                evt = new LogEvent(level, logger, "Invalid Object in Action Method", null);
            }

            // Write the event to the target
            await WriteLog(evt);
        }
    #endregion


    #region Implementation - Private functions
        protected async Task<bool> WriteLog(LogEvent evt)
        {
            // Get the contents to write to the log
            var contents = evt.Serialize();

            using (await _lock.LockAsync().ConfigureAwait(false))
            {
                // Get a stream writer to write to file
                using (var filewriter = await GetStreamForFile(kLogName))
                {
                    try
                    {
                        // Write to the file
                        await filewriter.WriteLineAsync(contents);
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }
            }

            return true;
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

        #endregion

        #region Action Function

        public static void LogAction(object obj, LoggingLevel level)
        {
            LoggingServices.Instance.LogMessage(obj, level);
        }
        #endregion

    }
}
