using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Party.Shared.Commands;
using Party.Shared.Discovery;

namespace Party.CLI.Commands
{
    public class ListScriptsCommand : HandlerBase
    {
        [Verb("scripts", HelpText = "Show all scripts")]
        public class Options : HandlerBase.CommonOptions
        {
            [Option("online", Default = false, HelpText = "Include online scripts from your registries")]
            public bool Online { get; set; }

            [Option("scenes", Default = false, HelpText = "Show scenes in which scripts are used")]
            public bool Scenes { get; set; }
        }

        public ListScriptsCommand(PartyConfiguration config, TextWriter output) : base(config, output)
        {
        }

        public async Task<int> ExecuteAsync(Options opts)
        {
            var config = GetConfig(opts, Config);
            var savesDirectory = config.VirtAMate.SavesDirectory;
            var ignore = config.Scanning.Ignore;
            var map = await SavesResolver.Resolve(SavesScanner.Scan(savesDirectory, ignore)).ConfigureAwait(false);

            Output.WriteLine("Scripts:");
            foreach (var scriptMap in map.ScriptMaps.OrderBy(sm => sm.Key))
            {
                Output.WriteLine($"- {scriptMap.Value.Name} ({Pluralize(scriptMap.Value.Scripts.Count(), "copy", "copies")} used by {Pluralize(scriptMap.Value.Scenes.Count(), "scene", "scenes")})");

                if (opts.Scenes)
                {
                    foreach (var scene in scriptMap.Value.Scenes)
                    {
                        Output.WriteLine($"  - {scene.Location.RelativePath}");
                    }
                }
            }

            return 0;
        }
    }
}
