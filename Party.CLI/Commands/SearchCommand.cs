using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Party.Shared.Commands;

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

        public static Command CreateCommand(IRenderer output, PartyConfiguration config)
        {
            var command = new Command("search", "Search for scripts and packages in the registry");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("query", null));
            command.AddOption(new Option("--show", "Include usage information from scenes") { Argument = new Argument<ShowOptions>(() => ShowOptions.ScriptOnly) });

            command.Handler = CommandHandler.Create(async (string saves, string query, ShowOptions show) =>
            {
                await new SearchCommand(output, config, saves).ExecuteAsync(query, show);
            });
            return command;
        }

        public SearchCommand(IRenderer output, PartyConfiguration config, string saves) : base(output, config, saves)
        {
        }


        private async Task ExecuteAsync(string query, ShowOptions show)
        {
            var command = new SearchPackagesHandler(Config);

            var results = new List<SearchPackagesHandler.SearchResult>();
            await foreach (var result in command.ExecuteAsync(query, show != ShowOptions.ScriptOnly).ConfigureAwait(false))
            {
                var script = result.Script;
                var latestVersion = script.GetLatestVersion();
                var trustNotice = result.Trusted ? "" : " [NOT TRUSTED]";
                var scenes = show != ShowOptions.ScriptOnly ? "" : $" (used in {Pluralize(result.Scenes?.Length ?? 0, "scene", "scenes")}";
                await Output.WriteLineAsync($"{script.Name} {latestVersion.Version ?? "-"} by {script.Author.Name}{trustNotice}");
                if (show == ShowOptions.ScenesList)
                {
                    if ((result.Scenes?.Length ?? 0) == 0)
                    {
                        await Output.WriteLineAsync("- Not used by any scenes");
                    }
                    else
                    {
                        foreach (var scene in result.Scenes)
                        {
                            // TODO: Show the version used in each scene
                            await Output.WriteLineAsync($"- {scene.Location.RelativePath}");
                        }
                    }
                }
            }
        }
    }
}
