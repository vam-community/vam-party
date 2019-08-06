using System;
using System.Collections.Concurrent;

namespace Party.Shared
{
    public interface IHashCache
    {
        string GetOrCreate(string fullPath, Func<string, string> create);
    }

    public class HashCache : IHashCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();
        public string GetOrCreate(string fullPath, Func<string, string> create)
        {
            return _cache.GetOrAdd(fullPath, create);
        }
    }

    public class NoHashCache : IHashCache
    {
        public string GetOrCreate(string fullPath, Func<string, string> create)
        {
            return create(fullPath);
        }
    }
}
