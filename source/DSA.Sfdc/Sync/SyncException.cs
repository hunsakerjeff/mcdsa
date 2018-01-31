using System;

namespace DSA.Sfdc.Sync
{
    public class SyncException : Exception
    {
        public SyncException()
        { }

        public SyncException(string message) : base(message) {  }

        public SyncException(string message, Exception innerEx) : base(message, innerEx) { }
    }
}
