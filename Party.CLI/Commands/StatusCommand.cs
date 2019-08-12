using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class StatusCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer output, PartyConfiguration config, PartyController controller)
        {
            var command = new Command("status", "Shows the state of the current scripts and scenes");
            AddCommonOptions(command);
            command.AddOption(new Option("--scenes", "Show scenes information") { Argument = new Argument<string>() });

            command.Handler = CommandHandler.Create(async (string saves, bool scenes) =>
            {
                await new StatusCommand(output, config, saves, controller).ExecuteAsync(scenes);
            });
            return command;
        }

        public StatusCommand(IRenderer output, PartyConfiguration config, string saves, PartyController controller) : base(output, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(bool scenes)
        {
            var saves = await Controller.GetSavesAsync();

            await Output.WriteLineAsync("Scripts:");
            foreach (var scriptMap in saves.ScriptMaps.OrderBy(sm => sm.Key))
            {
                await Output.WriteLineAsync($"- {scriptMap.Value.Name} ({Pluralize(scriptMap.Value.Scripts.Count(), "copy", "copies")} used by {Pluralize(scriptMap.Value.Scenes.Count(), "scene", "scenes")})");

                if (scenes)
                {
                    foreach (var scene in scriptMap.Value.Scenes)
                    {
                        await Output.WriteLineAsync($"  - {scene.Location.RelativePath}");
                    }
                }
            }
        }
    }
}
