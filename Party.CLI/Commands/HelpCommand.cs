using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class HelpCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("help", "Show useful information about party")
            {
                Handler = CommandHandler.Create<HelpArguments>(args => new HelpCommand(renderer, config, controller, args).ExecuteAsync())
            };
            return command;
        }

        public class HelpArguments : CommonArguments
        {
        }

        public HelpCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync()
        {
            var current = $"v{typeof(HelpCommand).Assembly.GetName().Version}";
            var latest = await Controller.GetPartyUpdatesAvailable();
            Renderer.WriteLine($"Party, {current}");
            if (latest != null && latest != current)
                Renderer.WriteLine($"An update is available: {latest}", ConsoleColor.Green);
            else if (latest == current)
                Renderer.WriteLine($"You are running the latest version!");
            Renderer.WriteLine("For help on available commands, type party -h");
            Renderer.WriteLine("For documentation on available commands, see: https://github.com/vam-community/vam-party/blob/master/USAGE.md");
            Renderer.WriteLine("For instructions on publishing to the registry, see: https://github.com/vam-community/vam-registry/blob/master/INSTRUCTIONS.md");
            Renderer.WriteLine("To report a bug, please file an issue here: https://github.com/vam-community/vam-party/issues");
        }
    }
}
