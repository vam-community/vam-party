using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.CLI.Commands;
using Party.Shared.Commands;

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
            var config = new PartyConfiguration();
            rootConfig.Bind(config);

            return CommandLine.Parser.Default.ParseArguments<ListScenesCommand.Options, ListScriptsCommand.Options, PackageScriptsCommand.Options, SearchScriptsCommand.Options>(args)
              .MapResult(
                (ListScenesCommand.Options opts) => ListScenesCommand.ExecuteAsync(opts, rootConfig).Result,
                (ListScriptsCommand.Options opts) => ListScriptsCommand.ExecuteAsync(opts, rootConfig).Result,
                (PackageScriptsCommand.Options opts) => PackageScriptsCommand.ExecuteAsync(opts, rootConfig).Result,
                (SearchScriptsCommand.Options opts) => SearchScriptsCommand.ExecuteAsync(opts, config).Result,
                errs => 1);
        }
    }
}
