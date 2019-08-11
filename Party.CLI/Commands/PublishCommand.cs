using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Party.Shared.Commands;

namespace Party.CLI.Commands
{
    public class PublishCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer output, PartyConfiguration config)
        {
            var command = new Command("publish", "Prepares files for publishing");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("input", null));

            command.Handler = CommandHandler.Create(async (string saves, string input) =>
            {
                await new PublishCommand(output, config, saves).ExecuteAsync(input);
            });
            return command;
        }

        public PublishCommand(IRenderer output, PartyConfiguration config, string saves) : base(output, config, saves)
        {
        }

        private async Task ExecuteAsync(string input)
        {
            var command = new BuildPackageJsonHandler(Config);

            var result = await command.ExecuteAsync(Path.GetFullPath(input)).ConfigureAwait(false);

            await Output.WriteLineAsync("JSON Template:");
            await Output.WriteLineAsync(result.Formatted);
        }
    }
}
