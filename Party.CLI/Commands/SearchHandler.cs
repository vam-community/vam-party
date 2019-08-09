using System;
using System.Threading.Tasks;
using CommandLine;
using Party.Shared.Commands;

namespace Party.CLI.Commands
{
    public class SearchHandler : HandlerBase
    {
        [Verb("search", HelpText = "Search remote scripts from the registry and locally")]
        public class Options : HandlerBase.CommonOptions
        {
            [Value(0, MetaName = "filter", Required = false, HelpText = "Filter the output")]
            public string Filter { get; set; }

            [Option('l', "local", Default = false, HelpText = "Check whether a script is used locally")]
            public bool Local { get; set; }
        }

        public SearchHandler(PartyConfiguration config) : base(config)
        {
        }

        public async Task<int> ExecuteAsync(Options opts)
        {
            var command = new SearchCommand(GetConfig(opts, Config));

            await foreach (var result in command.ExecuteAsync(opts.Filter, opts.Local))
            {
                var trustedMsg = result.Trusted ? "" : " [NOT TRUSTED]";
                var usedMsg = opts.Local
                    ? ((result.Scenes?.Length ?? 0) > 0 ? $" (referenced in {result.Scenes.Length} scenes)" : " (not referenced)")
                    : "";
                Console.WriteLine($"- {result.Script.Name} by {result.Script.Author.Name} (v{result.Script.GetLatestVersion().Version}){trustedMsg}{usedMsg}");
            }

            return 0;
        }
    }
}
