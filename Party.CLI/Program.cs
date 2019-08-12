using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Party.CLI.Commands;
using Party.Shared;

namespace Party.CLI
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootConfig = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("party.settings.json", optional: true, reloadOnChange: false)
                .Build();
            var config = DefaultConfiguration.Get();
            rootConfig.Bind(config);
            config.VirtAMate.SavesDirectory = Path.GetFullPath(config.VirtAMate.SavesDirectory, AppContext.BaseDirectory);

            var output = new ConsoleRenderer(Console.Out);

            var controller = new PartyController(config);

            var rootCommand = new RootCommand("Party: A Virt-A-Mate Package Manager") {
                SearchCommand.CreateCommand(output, config, controller),
                StatusCommand.CreateCommand(output, config, controller),
                PublishCommand.CreateCommand(output, config, controller)
            };

            // For CoreRT:
            rootCommand.Name = Path.GetFileName(Environment.GetCommandLineArgs().FirstOrDefault()) ?? "party.exe";

            await rootCommand.InvokeAsync(args);

            return 0;
        }
    }
}
