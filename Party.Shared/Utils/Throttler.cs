using System;
using System.Threading;
using System.Threading.Tasks;

namespace Party.Shared.Utils
{
    public class Throttler
    {
        public const int MaxConcurrentIO = 4;

        private readonly SemaphoreSlim _ioThrottler = new SemaphoreSlim(MaxConcurrentIO);

        public async Task<IDisposable> ThrottleIO()
        {
            await _ioThrottler.WaitAsync().ConfigureAwait(false);
            return new ReleaseWrapper(_ioThrottler);
        }

        private class ReleaseWrapper : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            private bool _isDisposed;

            public ReleaseWrapper(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _semaphore.Release();
                _isDisposed = true;
            }
        }
    }
}
