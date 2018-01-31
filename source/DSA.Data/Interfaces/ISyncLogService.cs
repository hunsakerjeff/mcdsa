using System;
using DSA.Model.Enums;

namespace DSA.Data.Interfaces
{
    public interface ISyncLogService
    {
        void StartSynchronization(SynchronizationMode syncType);

        void SynchronizationFailed(Exception exception);

        void SynchronizationCanceled();

        void SynchronizationCompleted();
    }
}