using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Party.Shared
{
    public class ProgressReporter<T> : IProgressReporter<T>, IDisposable
    {
        private readonly object _sync = new object();
        private readonly BlockingCollection<T> _queue = new BlockingCollection<T>(1);
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Action<T> _receive;
        private readonly Action _complete;
        private readonly Thread _thread;

        public ProgressReporter(Action start, Action<T> receive, Action complete)
        {
            _receive = receive ?? throw new ArgumentNullException(nameof(receive));
            _complete = complete ?? throw new ArgumentNullException(nameof(complete));
            _thread = new Thread(new ThreadStart(Receiver));
            _stopwatch.Start();
            _thread.Start();
            start();
        }

        public void Notify(T item)
        {
            _queue.TryAdd(item);
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _thread.Join(100);
            _queue.Dispose();
            _complete();
        }

        private void Receiver()
        {
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                lock (_sync)
                {
                    _receive(item);
                }
            }
        }
    }

    public interface IProgressReporter<T>
    {
        void Notify(T item);
    }
}
