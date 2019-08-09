using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Party.Shared;
using Party.Shared.Commands;
using Party.Shared.Discovery;
using Party.Shared.Resources;

namespace Party.CLI.Commands
{
    public class PackageHandler : HandlerBase
    {
        [Verb("package", HelpText = "Provides a ready to use JSON for your scripts")]
        public class Options : HandlerBase.CommonOptions
        {
            [Value(0, MetaName = "script", Required = true, HelpText = "The path to the script, or script folder")]
            public string Script { get; set; }
        }

        public PackageHandler(PartyConfiguration config) : base(config)
        {
        }

        public async Task<int> ExecuteAsync(Options opts)
        {
            var config = GetConfig(opts, Config);
            var savesDirectory = config.VirtAMate.SavesDirectory;
            var path = Path.GetFullPath(opts.Script, Environment.CurrentDirectory);

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

            var fileNodes = new List<dynamic>();
            foreach (var resource in resources.OrderBy(s => s.Location.Filename))
            {
                fileNodes.Add(new
                {
                    filename = resource.Location.Filename,
                    url = "",
                    hash = new
                    {
                        type = "sha256",
                        value = await resource.GetHashAsync()
                    }
                });
            }

            var scriptNode = new
            {
                files = fileNodes
            };
            Console.WriteLine(JsonConvert.SerializeObject(scriptNode, Formatting.Indented));

            return 0;
        }

        private static string Pluralize(int count, string singular, string plural)
        {
            if (count == 1)
                return $"{count} {singular}";
            else
                return $"{count} {plural}";
        }
    }
}
