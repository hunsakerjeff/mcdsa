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
    class Logger
    {
        // //////////////////////////////////////////////////////////
        // Constants
        // //////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        string Name { get; }
        readonly LoggingConfig _config;
        static readonly Task<bool[]> EmptyResults = Task.FromResult(new bool[] { });


        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public Logger(string name, LoggingConfig config)
        {
            Name = name;

            // add a target with config...
            _config = config;
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public void Log(LoggingLevel level, object obj)
        {
            // Local Variables
            var msg = obj as string;
            var ex = obj as Exception;

            // Create a logEvent Object
            if (msg != null)            // String option    
            {
                Log(level, msg, null);
            }
            else if (ex != null)        // Exception Option
            {
                Log(level, "Exception: ", ex);
            }
            else                        // Undefined Option
            {
                Log(level, "Invalid Object in Action Method", null);
            }
        }

        public void Log(LoggingLevel logLevel, string message, Exception ex)
        {
            LogAsync(logLevel, message, ex);
        }

        public void Log(LoggingLevel logLevel, string message)
        {
            LogAsync(logLevel, message);
        }

        public bool IsEnabled()
        {
            return _config.GetTargets().Any();
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Internal Methods
        // //////////////////////////////////////////////////////////
        internal Task<bool[]> LogAsync(LoggingLevel logLevel, string message, Exception ex)
        {
            return LogInternal(logLevel, message, ex);
        }

        internal Task<bool[]> LogAsync(LoggingLevel logLevel, string message)
        {
            return LogInternal(logLevel, message, null);
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Protected Methods
        // //////////////////////////////////////////////////////////
        Task<bool[]> LogInternal(LoggingLevel level, string message, Exception ex)
        {
            try
            {
                if (_config.Enabled == false)
                {
                    return EmptyResults;
                }

                var targets = _config.GetTargets();
                if (!(targets.Any()))
                {
                    return EmptyResults;
                }


                // create an event entry and pass it through...
                var entry = new LogEvent(level, Name, message, ex);

                // gather the tasks...
                var writeTasks = from target in targets select target.WriteLog(entry);

                // group...
                var group = Task.WhenAll(writeTasks);
                return group;
            }
            catch (Exception logEx)
            {
                return EmptyResults;
            }
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Private Methods
        // //////////////////////////////////////////////////////////

        // Create a logger object
        // Register it with an SDK service
        // Set Action function in Platform Adapter
        // In Action Function
        //      Call Service to get Logger
        //      Make Log call based on current Logger settings

    }
}




/*
         // CTOR
        public Logger()
        {
            // UWP is very restrictive of where you can save files on the disk.  The preferred place to do that is app's local folder.
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string logFullPath = $"{folder.Path}\\Logs\\DSA.log";


//            LogManagerFactory.DefaultConfiguration.AddTarget(MetroLog.LogLevel.Trace, LogLevel.Fatal, new StreamingFileTarget {REtain new FileStreamingTarget());

//#if DEBUG
//            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new FileStreamingTarget());
//            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new SQLiteTarget());
//#else
//        LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new FileStreamingTarget());
//        LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new SQLiteTarget());
//#endif


//            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new FileStreamingTarget());

//            //ILogger log = LogManagerFactory.DefaultLogManager.GetLogger<MainPage>();
//            ILogger log = LogManagerFactory.DefaultLogManager.GetLogger();

//            log.Trace("This is a trace message.");
        }


        // Properties
        public bool Enabled { get; set; } = false;
        public LoggingLevel LogLevel { get; set; } = LoggingLevel.Critical;
        public LoggingLevel Retain { get; set; } = LoggingLevel.Critical;


        // Implementation - Public Functions
        public void LogMessage(object obj, LoggingLevel level)
        {
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
            if (msg != null)
            {
                CreateLogEntry(msg, level);
                return;
            }

            var ex = obj as Exception;
            if (ex != null)
            {
                CreateLogEntry(ex, level);
                return;
            }

            // Unknown - can't log
            CreateLogEntry("Invalid Log Object", level);
        }

        // Implementation - Public Static Functions
        public static void LogAction(object obj, LoggingLevel level)
        {
            // Get the Logger
            Logger logger = new Logger();

            // Call Log Message
            logger.LogMessage(obj, level);
        }


        // Implementation - Private Functions
        public void CreateLogEntry(string input, LoggingLevel level)
        {
            string msg = String.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}", DateTime.Now, level.ToString(), input);
            LogMessage(msg);
        }

        public void CreateLogEntry(Exception ex, LoggingLevel level)
        {
            string msg = String.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] EX:{2}", DateTime.Now, level.ToString(), ex.ToString());
            LogMessage(msg);
        }

        public async void LogMessage(string msg)
        {
            // Create sample file; replace if exists.
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

            // Check if log file exists, open if there, create if not
            bool exists = await FileExists(kSuffix);

            try
            {
                StorageFile logFile;
                if (exists)
                {
                    logFile = await storageFolder.GetFileAsync(kSuffix);
                }
                else
                {
                    logFile = await storageFolder.CreateFileAsync(kSuffix, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                }

                // Write the Message
                await FileIO.WriteTextAsync(logFile, msg);
            }
            catch (Exception ex)
            {
                // Issue
            }
        }

        private async Task<bool> FileExists(string fileName)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
 
 */