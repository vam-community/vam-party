using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Resources;

namespace Party.Shared.Discovery
{
    public class SavesResolver
    {
        public SavesResolver()
        {

        }

        public async Task<SavesMap> Resolve(IEnumerable<Resource> resources)
        {
            var all = resources.ToList();
            var map = new SavesMap();

            foreach (var script in all.OfType<Script>())
            {
                var scriptMap = map.ScriptMaps.GetOrAdd(script.GetIdentifier(), _ => new ScriptMap(script.Location.Filename));
                scriptMap.Scripts.Add(script);
            }

            foreach (var scene in all.OfType<Scene>())
            {
                await foreach (var scriptRef in scene.GetScriptsAsync().ConfigureAwait(false))
                {
                    if (map.ScriptMaps.TryGetValue(scriptRef.GetIdentifier(), out var scriptMap))
                    {
                        scriptMap.Scenes.Add(scene);
                    }
                }
            }

            foreach (var scriptList in all.OfType<ScriptList>())
            {
                var scriptListMap = map.ScriptMaps.GetOrAdd(scriptList.GetIdentifier(), _ => new ScriptMap(scriptList.Location.Filename));

                await foreach (var scriptRef in scriptList.GetScriptsAsync().ConfigureAwait(false))
                {
                    if (map.ScriptMaps.TryRemove(scriptRef.GetIdentifier(), out var scriptMap))
                    {
                        foreach (var scene in scriptMap.Scenes)
                        {
                            // TODO: Shared instance by path using a mega dictionary
                            if (!scriptListMap.Scenes.Any(s => s.Location.RelativePath == scene.Location.RelativePath))
                                scriptListMap.Scenes.Add(scene);
                        }
                        foreach (var script in scriptMap.Scripts)
                        {
                            // TODO: Shared instance by path using a mega dictionary
                            if (!scriptListMap.Scripts.Any(s => s.Location.RelativePath == script.Location.RelativePath))
                                scriptListMap.Scripts.Add(script);
                        }
                    }
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
        public string Name { get; }

        public ConcurrentBag<ScriptList> ScriptLists { get; } = new ConcurrentBag<ScriptList>();
        public ConcurrentBag<Script> Scripts { get; } = new ConcurrentBag<Script>();
        public ConcurrentBag<Scene> Scenes { get; } = new ConcurrentBag<Scene>();

        public ScriptMap(string name)
        {
            // TODO: To avoid ADD_ME.cslist, we should choose between: Registry name, folder name, first script name, etc.
            Name = name;
        }
    }

}
