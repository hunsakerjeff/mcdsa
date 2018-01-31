using System;
using System.Threading.Tasks;

namespace DSA.Model.Messages
{
    public class SynchronizationCancelMessage
    {
        public Task<bool> Task { get; private set; }
        public Exception Exception { get; private set; }

        public SynchronizationCancelMessage(Task<bool> task, Exception exception)
        {
            Task = task;
            Exception = exception;
        }
    }
}
