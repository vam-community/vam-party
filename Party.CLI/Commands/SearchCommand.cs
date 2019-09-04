using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;

namespace Party.CLI.Commands
{
    public class SearchCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("search", "Search for scripts and packages in the registry");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("query", null));
            command.AddOption(new Option("--no-usage", "Do not show usage information from scenes (runs faster)"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));

            command.Handler = CommandHandler.Create<SearchArguments>(async args =>
            {
                await new SearchCommand(renderer, config, args.VaM, controller).ExecuteAsync(args);
            });
            return command;
        }

        public class SearchArguments : CommonArguments
        {
            public string Query { get; set; }
            public bool NoUsage { get; set; }
            public bool Warnings { get; set; }
        }

        public SearchCommand(IConsoleRenderer renderer, PartyConfiguration config, DirectoryInfo vam, IPartyController controller)
            : base(renderer, config, vam, controller)
        {
        }

        private async Task ExecuteAsync(SearchArguments args)
        {
            Controller.HealthCheck();

            var registryTask = Controller.GetRegistryAsync();
            var savesTask = args.NoUsage ? Task.FromResult<SavesMap>(null) : Controller.GetSavesAsync();
            await Task.WhenAll();
            var registry = await registryTask;
            var saves = await savesTask;

            PrintWarnings(args.Warnings, saves?.Errors);

            foreach (var result in Controller.Search(registry, saves, args.Query))
            {
                var script = result.Package;
                var latestVersion = script.GetLatestVersion();
                var trustNotice = result.Trusted ? "" : " [NOT TRUSTED]";

                Renderer.Write(script.Name, ConsoleColor.Blue);
                Renderer.Write($" v{latestVersion.Version}", ConsoleColor.Cyan);
                Renderer.Write($" by ");
                Renderer.Write(script.Author ?? "?", ConsoleColor.Magenta);
                if (!result.Trusted)
                {
                    Renderer.Write($" [NOT TRUSTED]", ConsoleColor.Red);
                }
                if (!args.NoUsage)
                {
                    Renderer.Write($" (used in {Pluralize(result.Scenes?.Length ?? 0, "scene", "scenes")})", ConsoleColor.DarkGray);
                }
                Renderer.WriteLine();
            }
        }
    }
}
