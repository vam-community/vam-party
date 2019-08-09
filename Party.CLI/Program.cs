using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.CLI.Commands;
using Party.Shared;

namespace Party.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var rootConfig = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("party.settings.json", optional: false, reloadOnChange: false)
                .Build();
            var config = DefaultConfiguration.Get();
            rootConfig.Bind(config);
            config.VirtAMate.SavesDirectory = Path.GetFullPath(config.VirtAMate.SavesDirectory, RuntimeUtilities.GetApplicationRoot());

            return CommandLine.Parser.Default.ParseArguments<ListScenesCommand.Options, ListScriptsCommand.Options, PackageHandler.Options, Commands.SearchHandler.Options>(args)
                .MapResult(
                    (ListScenesCommand.Options opts) => new ListScenesCommand(config).ExecuteAsync(opts).Result,
                    (ListScriptsCommand.Options opts) => new ListScriptsCommand(config).ExecuteAsync(opts).Result,
                    (PackageHandler.Options opts) => new PackageHandler(config).ExecuteAsync(opts).Result,
                    (Commands.SearchHandler.Options opts) => new SearchHandler(config).ExecuteAsync(opts).Result,
                    errs => 1);
        }
    }
}
