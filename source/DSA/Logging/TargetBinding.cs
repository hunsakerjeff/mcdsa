using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;


namespace DSA.Shell.Logging
{
    public class TargetBinding
    {
        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        internal LoggingLevel Level { get; }
        internal LogFileTarget Target { get; }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        internal TargetBinding(LoggingLevel level, LogFileTarget target)
        {
            Level = level;
            Target = target;
        }


        // //////////////////////////////////////////////////////////
        // Implementation - Internal Methods
        // //////////////////////////////////////////////////////////
        internal bool SupportsLevel(LoggingLevel level)
        {
            return (int)level >= (int)Level;
        }
    }
}
