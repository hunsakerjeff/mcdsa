using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesforce.SDK.Utilities
{
    public class NetworkErrorException : Exception
    {
        public static readonly string UserFriendlyMessage = "Synchronization failed due to too many concurrent users syncing at the same time.  Please try again";
    }
}
