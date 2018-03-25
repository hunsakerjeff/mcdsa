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
    public class Logger
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        protected readonly LoggingConfig _config;
        protected static readonly Task<bool[]> EmptyResults = Task.FromResult(new bool[] { });


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public Logger(LoggingConfig config)
        {
            // add a target with config...
            _config = config;
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public bool IsEnabled()
        {
            return _config.GetTargets(LoggingLevel.Critical).Any();
        }

        public void Log(LoggingLevel level, object obj)
        {
            // Local Variables
            var msg = obj as string;
            var ex = obj as Exception;

            // Create a logEvent Object
            if (msg != null)            // String option    
            {
                Log(level, msg);
            }
            else if (ex != null)        // Exception Option
            {
                Log(level, "Exception: ", ex);
            }
            else                        // Undefined Option
            {
                Log(level, "Invalid Object in Action Method");
            }
        }

        public void Log(LoggingLevel logLevel, string message)
        {
            LogAsync(logLevel, message);
        }

        public void Log(LoggingLevel logLevel, string message, Exception ex)
        {
            LogAsync(logLevel, message, ex);
        }

        public void Log(LoggingLevel logLevel, string message, params object[] ps)
        {
            LogAsync(logLevel, message, ps);
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Internal Methods
        // //////////////////////////////////////////////////////////
        internal Task<bool[]> LogAsync(LoggingLevel logLevel, string message, Exception ex = null)
        {
            return LogInternal(logLevel, message, null, ex, false);
        }

        internal Task<bool[]> LogAsync(LoggingLevel logLevel, string message, object[] ps)
        {
            return LogInternal(logLevel, message, ps, null, true);
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Protected Methods
        // //////////////////////////////////////////////////////////
        internal Task<bool[]> LogInternal(LoggingLevel level, string message, object[] ps, Exception ex, bool doFormat)
        {
            // Check state
            if (_config.Enabled == false)
            {
                return EmptyResults;
            }

            // Format for Global Crash handler
            if (doFormat)
            {
                message = string.Format(message, ps);
            }

            try
            {
                var targets = _config.GetTargets(level);
                if (!(targets.Any()))
                {
                    return EmptyResults;
                }

                // create an event entry and pass it through...
                var entry = new LogEvent(level, message, ex);

                // gather the tasks...
                var writeTasks = from target in targets select target.WriteLog(entry);

                // group...
                var group = Task.WhenAll(writeTasks);
                return group;
            }
            catch
            {
                return EmptyResults;
            }
        }
    }
}
