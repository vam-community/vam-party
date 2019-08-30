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

        internal async Task UpdateScripts(Scene scene, Script local, InstalledPackageInfoResult info)
        {
            var serializer = new SceneSerializer();

            var toUpdate = new List<(string before, string after)>
            {
                GetTransform(local, info)
            };

            if (local is ScriptList scriptList)
            {
                foreach (var script in scriptList.Scripts)
                {
                    toUpdate.Add(GetTransform(script, info));
                }
            }

            await serializer.UpdateScriptAsync(_fs, scene.FullPath, toUpdate);
        }

        private (string before, string after) GetTransform(Script local, InstalledPackageInfoResult info)
        {
            return
            (
                before: ToRelative(local.FullPath),
                after: info.Files.First(f => f.RegistryFile.Hash.Value == local.Hash).Path
            );
        }

        private string ToRelative(string path)
        {
            return path.Substring(_savesDirectory.Length).TrimStart(_fs.Path.DirectorySeparatorChar).Replace("\\", "/");
        }
    }
}
