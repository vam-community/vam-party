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
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            return CommandLine.Parser.Default.ParseArguments<ListScenesCommand.Options>(args)
              .MapResult(
                (ListScenesCommand.Options opts) => ListScenesCommand.Execute(opts),
                errs => 1);
        }
    }
}
