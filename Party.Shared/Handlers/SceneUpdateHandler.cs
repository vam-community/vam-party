using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Models;
using Party.Shared.Resources;
using Party.Shared.Serializers;

namespace Party.Shared.Handlers
{
    public class SceneUpdateHandler
    {
        private readonly ISceneSerializer _serializer;
        private readonly string _savesDirectory;

        public SceneUpdateHandler(ISceneSerializer serializer, string savesDirectory)
        {
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
            _serializer = serializer;
        }

        public async Task<(string before, string after)[]> UpdateScripts(Scene scene, Script local, InstalledPackageInfoResult info)
        {
            var changes = new List<(string before, string after)>();

            changes.AddRange(GetTransform(local, info));

            if (local is ScriptList scriptList)
            {
                changes.AddRange(scriptList.Scripts.SelectMany(script => GetTransform(script, info)));
            }

            var result = await _serializer.UpdateScriptAsync(scene.FullPath, changes);

            return result.ToArray();
        }

        private IEnumerable<(string before, string after)> GetTransform(Script local, InstalledPackageInfoResult info)
        {
            var after = ToRelative(info.Files.First(f => f.RegistryFile.Hash.Value == local.Hash).Path);
            yield return (before: ToRelative(local.FullPath), after);
            yield return (before: Path.GetFileName(local.FullPath), after);
        }

        private string ToRelative(string path)
        {
            return "Saves/" + path.Substring(_savesDirectory.Length).TrimStart(Path.DirectorySeparatorChar).Replace("\\", "/");
        }
    }
}
