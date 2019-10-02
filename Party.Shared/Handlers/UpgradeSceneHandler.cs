using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Serializers;

namespace Party.Shared.Handlers
{
    public class UpgradeSceneHandler
    {
        private readonly ISceneSerializer _serializer;
        private readonly IFoldersHelper _folders;

        public UpgradeSceneHandler(ISceneSerializer serializer, IFoldersHelper folders)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _folders = folders ?? throw new ArgumentNullException(nameof(folders));
        }

        public async Task<int> UpgradeSceneAsync(LocalSceneFile scene, LocalScriptFile local, LocalPackageInfo after)
        {
            if (scene is null) throw new ArgumentNullException(nameof(scene));
            if (local is null) throw new ArgumentNullException(nameof(local));
            if (after is null) throw new ArgumentNullException(nameof(after));

            var changes = DetermineChanges(local, after);
            if (changes.Count == 0) return 0;

            return await ApplyChanges(scene, changes).ConfigureAwait(false);
        }

        private Dictionary<string, string> DetermineChanges(LocalScriptFile local, LocalPackageInfo after)
        {
            var changes = new Dictionary<string, string>();
            if (after.Files.Length == 1)
            {
                AddChanges(changes, local.FullPath, after.Files[0].FullPath);
                return changes;
            }

            var sameFilename = after.Files.FirstOrDefault(f => Path.GetFileName(f.FullPath) == local.FileName);
            if (sameFilename != null)
            {
                AddChanges(changes, local.FullPath, sameFilename.FullPath);
                return changes;
            }

            throw new NotImplementedException("No automatic strategy implement for this upgrade type");
        }

        private async Task<int> ApplyChanges(LocalSceneFile scene, Dictionary<string, string> changes)
        {
            var counter = 0;
            var json = await _serializer.DeserializeAsync(scene.FullPath).ConfigureAwait(false);
            foreach (var plugins in json.Atoms.SelectMany(a => a.Plugins).GroupBy(p => p.Path))
            {
                if (changes.TryGetValue(plugins.Key, out var after))
                {
                    foreach (var plugin in plugins)
                    {
                        plugin.Path = after;
                        counter++;
                    }
                }
            }
            if (counter > 0)
                await _serializer.SerializeAsync(json, scene.FullPath).ConfigureAwait(false);
            return counter;
        }

        private void AddChanges(IDictionary<string, string> changes, string before, string after)
        {
            after = _folders.ToRelativeToVam(after).Replace("\\", "/");
            string absolute = _folders.ToRelativeToVam(before).Replace("\\", "/");
            changes.Add(absolute, after);
            // VaM 1.18 path changed, but old paths are still supported
            if (absolute.StartsWith("Custom/Scripts/"))
                changes.Add("Saves/Scripts/" + absolute.Substring("Custom/Scripts/".Length), after);
            changes.Add(Path.GetFileName(before), after);
        }
    }
}
