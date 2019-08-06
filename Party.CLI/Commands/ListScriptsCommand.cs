using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class ListScriptsCommand
    {
        [Verb("scripts", HelpText = "Show all scripts")]
        public class Options : CommonOptions
        {
            [Option("online", Default = false, HelpText = "Include online scripts from your registries")]
            public bool Online { get; set; }

            [Option("scenes", Default = false, HelpText = "Show scenes in which scripts are used")]
            public bool Scenes { get; set; }
        }

        public static Task<int> ExecuteAsync(Options opts, IConfiguration config)
        {
            var savesDirectory = Path.GetFullPath(opts.Saves ?? Path.Combine(Environment.CurrentDirectory, "Saves"));
            var ignore = config.GetSection("Scanning:Ignore").GetChildren().Select(x => x.Value).ToArray();
            var scripts = SavesScanner.Scan(savesDirectory, ignore).OfType<Script>().GroupBy(s => s.GetHash());

            Console.WriteLine("Scripts:");
            foreach (var script in scripts)
            {
                Console.WriteLine($"- {script.First().Location.Filename} ({script.Count()})");

                if (opts.Scenes)
                {
                    throw new NotImplementedException();
                }
            }

            return Task.FromResult(0);
        }
    }
}
