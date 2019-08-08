using System;
using System.Threading.Tasks;
using CommandLine;
using Party.Shared.Commands;

namespace Party.CLI.Commands
{
    public class SearchScriptsCommand
    {
        [Verb("search", HelpText = "Search remote scripts from the registry and locally")]
        public class Options : CommonOptions
        {
        }

        public static async Task<int> ExecuteAsync(Options opts, PartyConfiguration config)
        {
            // TODO: Extension on IConfiguration
            var command = new SearchCommand(config);
            await foreach (var result in command.ExecuteAsync())
            {
                 var trustedMsg = result.Trusted ? "" : " [NOT TRUSTED]";
                Console.WriteLine($"- {result.Script.Name} by {result.Script.Author.Name} (v{result.Script.GetLatestVersion().Version}){trustedMsg}");               
            }

            return 0;
        }
    }
}
