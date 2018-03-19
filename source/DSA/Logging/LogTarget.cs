using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Shell.Logging
{
    public abstract class LogTarget
    {
        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        protected LogTarget()
        {
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Internal Methods
        // //////////////////////////////////////////////////////////
        internal async Task<bool> WriteLog(LogEvent evt)
        {
            return await WriteLogInternal(evt).ConfigureAwait(false);
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Protected Abstract Methods
        // //////////////////////////////////////////////////////////
        protected abstract Task<bool> WriteLogInternal(LogEvent evt);
    }
}
