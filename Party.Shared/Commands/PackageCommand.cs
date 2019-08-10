using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Discovery;
using Party.Shared.Resources;

namespace Party.Shared.Commands
{
    public class PackageCommand : CommandBase
    {
        public class PackageResult
        {
            public string Formatted { get; internal set; }
        }

        public PackageCommand(PartyConfiguration config)
        : base(config)
        {
        }

        public async Task<PackageResult> ExecuteAsync(string path)
        {
            var savesDirectory = Config.VirtAMate.SavesDirectory;

            var attrs = File.GetAttributes(path);
            Resource[] resources;
            var types = new[] { "cs", "cslist" };
            var cache = new NoHashCache();
            if (attrs.HasFlag(FileAttributes.Directory))
            {
                resources = SavesScanner.Scan(path, new string[0]).Where(s => types.Contains(s.Type)).ToArray();
            }
            else if (attrs.HasFlag(FileAttributes.Normal))
            {
                resources = new[] { new Script(VamLocation.Absolute(savesDirectory, path), cache) };
            }
            else
            {
                throw new InvalidOperationException("Specified file is neither a directory nor a file");
            }

            var files = new List<Registry.RegistryFile>();
            foreach (var resource in resources.OrderBy(s => s.Location.Filename))
            {
                files.Add(new Registry.RegistryFile
                {
                    Filename = resource.Location.Filename,
                    Url = "",
                    Hash = new Registry.RegistryFileHash
                    {
                        Type = "sha256",
                        Value = await resource.GetHashAsync().ConfigureAwait(false)
                    }
                });
            }

            var scriptJson = new Registry.RegistryScript
            {
                Author = new Registry.RegistryScriptAuthor
                {
                    Name = "User Name",
                    Profile = "https://"
                },
                Name = files.Select(f => Path.GetFileNameWithoutExtension(f.Filename)).FirstOrDefault(),
                Homepage = "https://...",
                Repository = "https://...",
                Versions = new List<Registry.RegistryScriptVersion> {
                    new Registry.RegistryScriptVersion{
                        Version = "0.0.0",
                        Files = files
                    }
                }
            };

            return new PackageResult
            {
                Formatted = JsonConvert.SerializeObject(scriptJson, Formatting.Indented)
            };
        }
    }
}
