using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Party.CLI.Commands;
using Party.Shared;
using Party.Shared.Exceptions;

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
            var config = PartyConfigurationFactory.Create();
            rootConfig.Bind(config);
            config.VirtAMate.SavesDirectory = Path.GetFullPath(config.VirtAMate.SavesDirectory, AppContext.BaseDirectory);

            var output = new ConsoleRenderer(Console.Out);

            var controller = new PartyController(config);

            var rootCommand = new RootCommand("Party: A Virt-A-Mate package manager") {
                SearchCommand.CreateCommand(output, config, controller),
                StatusCommand.CreateCommand(output, config, controller),
                PublishCommand.CreateCommand(output, config, controller),
                GetCommand.CreateCommand(output, config, controller)
            };

            // For CoreRT:
            rootCommand.Name = Path.GetFileName(Environment.GetCommandLineArgs().FirstOrDefault()) ?? "party.exe";

            Exception exc = null;
            try
            {
                var parser = new CommandLineBuilder(rootCommand)
                       .UseVersionOption()
                       .UseHelp()
#if (DEBUG)
                       .UseParseDirective()
                       .UseDebugDirective()
#endif
                       .UseSuggestDirective()
                       .RegisterWithDotnetSuggest()
                       .UseTypoCorrections()
                       .UseParseErrorReporting()
                       .UseExceptionHandler((e, ctx) => exc = e)
                       .CancelOnProcessTermination()
                       .Build();

                await parser.InvokeAsync(args);
            }
            catch (Exception e)
            {
                exc = e;
            }

            if (exc != null)
            {
                return HandleError(exc);
            }

            return 0;
        }

        private static int HandleError(Exception exc)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            try
            {
                if (exc is PartyException partyExc)
                {
                    Console.Error.WriteLine(partyExc.Message);
                    return partyExc.Code;
                }

                if (exc is UnauthorizedAccessException unauthorizedExc)
                {
                    Console.Error.WriteLine(unauthorizedExc.Message);
                    return 2;
                }

                ExceptionDispatchInfo.Capture(exc).Throw();
                return 1;
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
