using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace DSA.Shell.Logging
{
    public static class LoggingServices
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        private static LoggingConfig    _defaultConfig = new LoggingConfig();
        private static LogManager       _logManager = null;


        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public static LoggingConfig DefaultConfiguration
        {
            get
            {
                return _defaultConfig;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Must not have created the LogManager
                if (null != _logManager)
                {
                    throw new InvalidOperationException("Must set DefaultConfiguration before any calls to DefaultLogManager");
                }

                _defaultConfig = value;
            }
        }

        public static LogManager DefaultLogManager
        {
            get
            {
                // If not created, create it
                if (null == _logManager)
                {
                    _logManager = CreateLogManager(DefaultConfiguration);
                }
                return _logManager;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _logManager = value;
            }
        }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        static LoggingServices()
        {
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public static LogManager CreateLogManager(LoggingConfig config = null)
        {
            var cfg = config ?? DefaultConfiguration;
            cfg.LockDown();

            return new LogManager(cfg);
        }

        public static LoggingConfig CreateLoggingConfig()
        {
            return new LoggingConfig();
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Action Function
        // //////////////////////////////////////////////////////////
        public static void LogAction(object obj, LoggingLevel level)
        {
            LogManager logManager = LoggingServices.DefaultLogManager;
            Logger logger = logManager.GetLogger();
            logger.Log(level, obj);
        }
    }
}
