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
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("search", "Search for scripts and packages in the registry");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("query", null));
            command.AddOption(new Option("--no-usage", "Do not show usage information from scenes (runs faster)"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string query, bool noUsage, bool warnings) =>
            {
                await new SearchCommand(renderer, config, saves, controller).ExecuteAsync(query, noUsage, warnings);
            });
            return command;
        }

        public SearchCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string query, bool noUsage, bool warnings)
        {
            var registryTask = Controller.GetRegistryAsync();
            var savesTask = noUsage ? Task.FromResult<SavesMap>(null) : Controller.GetSavesAsync();
            await Task.WhenAll();
            var registry = await registryTask;
            var saves = await savesTask;

            PrintWarnings(warnings, saves?.Errors);

            foreach (var result in Controller.Search(registry, saves, query))
            {
                var script = result.Package;
                var latestVersion = script.GetLatestVersion();
                var trustNotice = result.Trusted ? "" : " [NOT TRUSTED]";
                var scenes = noUsage ? "" : $" (used in {Pluralize(result.Scenes?.Length ?? 0, "scene", "scenes")})";
                Renderer.WriteLine($"{script.Name} {latestVersion.Version ?? "-"} by {script.Author.Name}{trustNotice}{scenes}");
            }
        }
    }
}
