using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class SearchCommand : CommandBase
    {
        public enum ShowOptions
        {
            ScriptOnly,
            ScenesCount,
            ScenesList
        }

        public static Command CreateCommand(IRenderer output, PartyConfiguration config, PartyController controller)
        {
            var command = new Command("search", "Search for scripts and packages in the registry");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("query", null));
            command.AddOption(new Option("--show", "Include usage information from scenes") { Argument = new Argument<ShowOptions>(() => ShowOptions.ScriptOnly) });

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string query, ShowOptions show) =>
            {
                await new SearchCommand(output, config, saves, controller).ExecuteAsync(query, show);
            });
            return command;
        }

        public SearchCommand(IRenderer output, PartyConfiguration config, DirectoryInfo saves, PartyController controller) : base(output, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string query, ShowOptions show)
        {
            var registryTask = Controller.GetRegistryAsync();
            var savesTask = Controller.GetSavesAsync();
            await Task.WhenAll();
            var registry = await registryTask;
            var saves = await savesTask;

            foreach (var result in Controller.Search(registry, saves, query, show != ShowOptions.ScriptOnly))
            {
                var script = result.Package;
                var latestVersion = script.GetLatestVersion();
                var trustNotice = result.Trusted ? "" : " [NOT TRUSTED]";
                var scenes = show != ShowOptions.ScriptOnly ? "" : $" (used in {Pluralize(result.Scenes?.Length ?? 0, "scene", "scenes")}";
                Output.WriteLine($"{script.Name} {latestVersion.Version ?? "-"} by {script.Author.Name}{trustNotice}");
                if (show == ShowOptions.ScenesList)
                {
                    if ((result.Scenes?.Length ?? 0) == 0)
                    {
                        Output.WriteLine("- Not used by any scenes");
                    }
                    else
                    {
                        foreach (var scene in result.Scenes)
                        {
                            // TODO: Show the version used in each scene
                            Output.WriteLine($"- {Controller.GetRelativePath(scene.FullPath)}");
                        }
                    }
                }
            }
        }
    }
}
