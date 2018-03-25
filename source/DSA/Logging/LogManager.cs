using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Shell.Logging
{
    public class LogManager
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        protected Logger _logger = null;
        protected readonly object _loggersLock = new object();


        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public LoggingConfig DefaultConfiguration { get; private set; }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public LogManager(LoggingConfig configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            DefaultConfiguration = configuration;
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public Logger GetLogger(LoggingConfig config = null)
        {
            lock (_loggersLock)
            {
                if (null == _logger)
                {
                    var logger = new Logger(config ?? DefaultConfiguration);
                    _logger = logger;
                }
            }

            return _logger;
        }
    }
}
