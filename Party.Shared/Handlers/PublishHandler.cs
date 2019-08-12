using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Discovery;
using Party.Shared.Resources;
using Party.Shared.Results;

namespace Party.Shared.Handlers
{
    public class PublishHandler
    {
        private readonly PartyConfiguration _config;

        public PublishHandler(PartyConfiguration config)
        {
            _config = config;
        }

        public async Task<PublishResult> ExecuteAsync(string path)
        {
            var savesDirectory = _config.VirtAMate.SavesDirectory;

            var attrs = File.GetAttributes(path);
            Resource[] resources;
            var types = new[] { "cs", "cslist" };
            var cache = new NoHashCache();
            if (attrs.HasFlag(FileAttributes.Directory))
            {
                resources = new SavesScanner(path, new string[0]).Scan().Where(s => types.Contains(s.Type)).ToArray();
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
                Description = "",
                Tags = new List<string>(new[] { "" }),
                Homepage = "https://...",
                Repository = "https://...",
                Versions = new List<Registry.RegistryScriptVersion> {
                    new Registry.RegistryScriptVersion{
                        Version = "0.0.0",
                        Files = files
                    }
                }
            };

            return new PublishResult
            {
                Formatted = JsonConvert.SerializeObject(scriptJson, Formatting.Indented)
            };
        }
    }
}
