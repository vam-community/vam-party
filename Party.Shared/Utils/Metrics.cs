using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Party.Shared.Utils
{
    public static class Metrics
    {
        public static async Task<(T, TimeSpan)> Measure<T>(Func<Task<T>> fn)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = await fn();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }
    }
}
