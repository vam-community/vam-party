using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class SearchCommand : CommandBase<SearchArguments>
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("search", "Search for packages in the registry");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("query", null));

            command.Handler = CommandHandler.Create<SearchArguments>(async args =>
            {
                await new SearchCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        public SearchCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        protected override async Task ExecuteImplAsync(SearchArguments args)
        {
            ValidateArguments(args.Query);
            Controller.HealthCheck();

            var registry = await Controller.AcquireRegistryAsync();

            foreach (var result in Controller.FilterRegistry(registry, args.Query))
            {
                var package = result.Package;
                var latestVersion = package.GetLatestVersion();

                Renderer.Write(package.Type.ToString().ToLowerInvariant(), ConsoleColor.Gray);
                Renderer.Write("/", ConsoleColor.DarkGray);
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

    public class SearchArguments : CommonArguments
    {
        public string Query { get; set; }
        public bool NoUsage { get; set; }
        public bool Errors { get; set; }
    }
}
