using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSA.Shell.Logging
{
    class AsyncLock
    {
        // //////////////////////////////////////////////////////////
        // Attributes
        // //////////////////////////////////////////////////////////
        readonly SemaphoreSlim m_semaphore;
        readonly Task<Releaser> m_releaser;

        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public AsyncLock()
        {
            m_semaphore = new SemaphoreSlim(1);
            m_releaser = Task.FromResult(new Releaser(this));
        }

        // //////////////////////////////////////////////////////////
        // Encapsulated Struct
        // //////////////////////////////////////////////////////////
        public struct Releaser : IDisposable
        {
            // Attributes
            readonly AsyncLock m_toRelease;

            // CTOR
            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

            // Public
            public void Dispose()
            {
                m_toRelease?.m_semaphore.Release();
            }
        }

        // //////////////////////////////////////////////////////////
        // Implementation - Public Methods
        // //////////////////////////////////////////////////////////
        public Task<Releaser> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ? m_releaser : wait.ContinueWith((_, state) => new Releaser((AsyncLock)state), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
