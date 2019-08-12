using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class PublishCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer output, PartyConfiguration config, PartyController controller)
        {
            var command = new Command("publish", "Prepares files for publishing");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("input", null));

            command.Handler = CommandHandler.Create(async (string saves, string input) =>
            {
                await new PublishCommand(output, config, saves, controller).ExecuteAsync(input);
            });
            return command;
        }

        public PublishCommand(IRenderer output, PartyConfiguration config, string saves, PartyController controller) : base(output, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string input)
        {
            var result = await Controller.Publish(input).ConfigureAwait(false);

            await Output.WriteLineAsync("JSON Template:");
            await Output.WriteLineAsync(result.Formatted);
        }
    }
}
