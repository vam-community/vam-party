using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Party.Shared
{
    public interface IHashCache
    {
        Task<string> GetOrCreate(string fullPath, Func<string, Task<string>> create);
    }

    public class HashCache : IHashCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();
        public async Task<string> GetOrCreate(string fullPath, Func<string, Task<string>> create)
        {
            if (_cache.TryGetValue(fullPath, out var hash))
                return hash;
            hash = await create(fullPath).ConfigureAwait(false);
            _cache.TryAdd(fullPath, hash);
            return hash;
        }
    }

    public class NoHashCache : IHashCache
    {
        public Task<string> GetOrCreate(string fullPath, Func<string, Task<string>> create)
        {
            return create(fullPath);
        }
    }
}
