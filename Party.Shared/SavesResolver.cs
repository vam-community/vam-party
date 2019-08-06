using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Party.Shared
{
    public class SavesResolver
    {
        public static async Task<SavesMap> Resolve(IEnumerable<Resource> resources)
        {
            var all = resources.ToList();
            var map = new SavesMap();

            foreach (var resource in all)
            {
                switch (resource)
                {
                    case Script script:
                        var scriptMap = map.ScriptMaps.GetOrAdd(script.GetIdentifier(), _ => new ScriptMap());
                        scriptMap.Scripts.Add(script);
                        break;
                }
            }

            foreach (var resource in all)
            {
                switch (resource)
                {
                    case Scene scene:
                        await foreach (var scriptRef in scene.GetScriptsAsync())
                        {
                            if (map.ScriptMaps.TryGetValue(scriptRef.GetIdentifier(), out var scriptMap))
                            {
                                scriptMap.Scenes.Add(scene);
                            }
                        }
                        break;
                }
            }

            return map;
        }
    }

    public class SavesMap
    {
        public ConcurrentDictionary<string, ScriptMap> ScriptMaps { get; } = new ConcurrentDictionary<string, ScriptMap>();
    }

    public class ScriptMap
    {
        public ConcurrentBag<Script> Scripts { get; } = new ConcurrentBag<Script>();
        public ConcurrentBag<Scene> Scenes { get; } = new ConcurrentBag<Scene>();
    }

}
