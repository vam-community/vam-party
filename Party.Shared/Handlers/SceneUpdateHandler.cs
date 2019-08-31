using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Models;
using Party.Shared.Resources;
using Party.Shared.Serializers;

namespace Party.Shared.Handlers
{
    public class SceneUpdateHandler
    {
        private readonly IFileSystem _fs;
        private readonly string _savesDirectory;

        public SceneUpdateHandler(IFileSystem fs, string savesDirectory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
        }

        internal async Task<(string before, string after)[]> UpdateScripts(Scene scene, Script local, InstalledPackageInfoResult info)
        {
            var serializer = new SceneSerializer();

            var changes = new List<(string before, string after)>(GetTransform(local, info));

            if (local is ScriptList scriptList)
            {
                changes.AddRange(scriptList.Scripts.SelectMany(script => GetTransform(script, info)));
            }

            var result = await serializer.UpdateScriptAsync(_fs, scene.FullPath, changes);

            return result.ToArray();
        }

        private IEnumerable<(string before, string after)> GetTransform(Script local, InstalledPackageInfoResult info)
        {
            var after = ToRelative(info.Files.First(f => f.RegistryFile.Hash.Value == local.Hash).Path);
            yield return (before: ToRelative(local.FullPath), after);
            yield return (before: _fs.Path.GetFileName(local.FullPath), after);
        }

        private string ToRelative(string path)
        {
            return "Saves/" + path.Substring(_savesDirectory.Length).TrimStart(_fs.Path.DirectorySeparatorChar).Replace("\\", "/");
        }
    }
}
