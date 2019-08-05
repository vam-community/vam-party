using System;
using System.Collections.Concurrent;

namespace Party.Shared
{
    public interface IScriptHashCache
    {
        string GetOrCreate(string fullPath, Func<string, string> create);
    }

    public class ScriptHashCache : IScriptHashCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();
        public string GetOrCreate(string fullPath, Func<string, string> create)
        {
            return _cache.GetOrAdd(fullPath, create);
        }
    }

    public class NoScriptHashCache : IScriptHashCache
    {
        public string GetOrCreate(string fullPath, Func<string, string> create)
        {
            return create(fullPath);
        }
    }
}
