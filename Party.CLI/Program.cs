using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.CLI.Commands;

namespace Party.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("party.settings.json", optional: false, reloadOnChange: false)
                .Build();

            return CommandLine.Parser.Default.ParseArguments<ListScenesCommand.Options, ListScriptsCommand.Options, PackageScriptsCommand.Options>(args)
              .MapResult(
                (ListScenesCommand.Options opts) => ListScenesCommand.ExecuteAsync(opts, config).Result,
                (ListScriptsCommand.Options opts) => ListScriptsCommand.ExecuteAsync(opts, config).Result,
                (PackageScriptsCommand.Options opts) => PackageScriptsCommand.ExecuteAsync(opts, config).Result,
                errs => 1);
        }
    }
}
