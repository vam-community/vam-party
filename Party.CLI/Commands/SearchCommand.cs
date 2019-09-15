using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;

namespace Party.CLI.Commands
{
    public class SearchCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("search", "Search for packages in the registry");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("query", null));

            command.Handler = CommandHandler.Create<SearchArguments>(async args =>
            {
                await new SearchCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class SearchArguments : CommonArguments
        {
            public string Query { get; set; }
            public bool NoUsage { get; set; }
            public bool Warnings { get; set; }
        }

        public SearchCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(SearchArguments args)
        {
            Controller.HealthCheck();

            var registry = await Controller.GetRegistryAsync();

            foreach (var result in Controller.Search(registry, args.Query))
            {
                var package = result.Package;
                var latestVersion = package.GetLatestVersion();

                Renderer.Write(package.Name, ConsoleColor.Blue);
                Renderer.Write($" v{latestVersion.Version}", ConsoleColor.Cyan);
                Renderer.Write($" by ");
                Renderer.Write(package.Author ?? "?", ConsoleColor.Magenta);
                if (!result.Trusted)
                {
                    Renderer.Write($" [NOT TRUSTED]", ConsoleColor.Red);
                }
                Renderer.WriteLine();
            }
        }
    }
}
