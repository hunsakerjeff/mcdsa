using System;
using System.Collections.Generic;
using Windows.Foundation.Diagnostics;


namespace DSA.Shell.Logging
{
    class LoggingConfig
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        readonly List<TargetBinding> _bindings;
        readonly object _bindingsLock = new object();
        bool _lockdown;

        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public bool Enabled { get; set; }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public LoggingConfig()
        {
            Enabled = true; // default to true to enable logging
            _bindings = new List<TargetBinding>();
            _lockdown = false;
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public void AddTarget(LoggingLevel level, LogFileTarget target)
        {
            if (_lockdown)
            {
                throw new InvalidOperationException("Cannot modify config after initialization");
            }

            lock (_bindingsLock)
            {
                _bindings.Add(new TargetBinding(level, target));
            }
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Internal Methods
        // //////////////////////////////////////////////////////////
        internal IEnumerable<LogFileTarget> GetTargets()
        {
            lock (_bindings)
            {
                var results = new List<LogFileTarget>();
                foreach (var binding in _bindings)
                {
                    results.Add(binding.Target);
                }

                return results;
            }
        }

        internal void LockDown()
        {
            _lockdown = true;
        }
    }
}
