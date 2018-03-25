using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Diagnostics;


namespace DSA.Shell.Logging
{
    public class LoggingConfig
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        protected readonly List<TargetBinding> _bindings = null;
        protected readonly object _bindingsLock = new object();
        protected bool _lockdown = false;


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
        internal IEnumerable<LogFileTarget> GetTargets(LoggingLevel level)
        {
            lock (_bindingsLock)
            {
                return _bindings.Where(v => v.SupportsLevel(level)).Select(_binding => _binding.Target).ToList();
            }
        }

        internal void LockDown()
        {
            _lockdown = true;
        }
    }
}
