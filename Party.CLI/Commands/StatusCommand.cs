using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, bool scenes) =>
            {
                await new StatusCommand(output, config, saves, controller).ExecuteAsync(scenes);
            });
            return command;
        }

        public StatusCommand(IRenderer output, PartyConfiguration config, DirectoryInfo saves, PartyController controller) : base(output, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(bool scenes)
        {
            var saves = await Controller.GetSavesAsync();

            Output.WriteLine("Scripts:");
            foreach (var script in saves.Scripts.OrderBy(sm => sm.FullPath))
            {
                Output.WriteLine($"- {script.Name} (used in {Pluralize(script.Scenes?.Count() ?? 0, "scene", "scenes")})");

                if (scenes && script.Scenes != null)
                {
                    foreach (var scene in script.Scenes)
                    {
                        Output.WriteLine($"  - {Controller.GetRelativePath(scene.FullPath)}");
                    }
                }
            }
        }
    }
}
