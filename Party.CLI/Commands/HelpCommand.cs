using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Party.Shared;

namespace Party.CLI.Commands
{
    public class HelpCommand : CommandBase<HelpArguments>
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            return new Command("help", "Show useful information about party")
            {
                Handler = CommandHandler.Create<HelpArguments>(args =>
                    new HelpCommand(renderer, config, controllerFactory, args).ExecuteAsync(args))
            };
        }

        public HelpCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        protected override async Task ExecuteImplAsync(HelpArguments args)
        {
            var current = $"v{typeof(HelpCommand).Assembly.GetName().Version}";
            var latest = await Controller.GetPartyUpdatesAvailableAsync();
            Renderer.WriteLine($"Party, {current}");
            if (latest != null && latest != current)
                Renderer.WriteLine($"An update is available: {latest}", ConsoleColor.Green);
            else if (latest == current)
                Renderer.WriteLine($"You are running the latest version!");
            Renderer.WriteLine("For help on available commands, type party -h");
            Renderer.WriteLine("For documentation on available commands, see: https://github.com/vam-community/vam-party/blob/master/USAGE.md");
            Renderer.WriteLine("For instructions on publishing to the registry, see: https://github.com/vam-community/vam-registry/blob/master/PUBLISHING.md");
            Renderer.WriteLine("To report a bug, please file an issue here: https://github.com/vam-community/vam-party/issues");
        }
    }

    public class HelpArguments : CommonArguments
    {
    }
}
