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
            var config = PartyConfigurationFactory.Create(AppContext.BaseDirectory);
            rootConfig.Bind(config);

            var renderer = new ConsoleRenderer(Console.Out, Console.In, Console.Error, (ConsoleColor color) => Console.ForegroundColor = color, () => Console.ResetColor());

            var controller = new PartyController(config);

            return await new Program(renderer, config, controller).Execute(args).ConfigureAwait(false);
        }

        private readonly IRenderer _renderer;
        private readonly PartyConfiguration _config;
        private readonly IPartyController _controller;

        public Program(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            _renderer = renderer;
            _config = config;
            _controller = controller;
        }

        public async Task<int> Execute(string[] args)
        {

            var rootCommand = new RootCommand("Party: A Virt-A-Mate package manager") {
                SearchCommand.CreateCommand(_renderer, _config, _controller),
                GetCommand.CreateCommand(_renderer, _config, _controller),
                ShowCommand.CreateCommand(_renderer, _config, _controller),
                StatusCommand.CreateCommand(_renderer, _config, _controller),
                PublishCommand.CreateCommand(_renderer, _config, _controller),
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
                       //.RegisterWithDotnetSuggest()
                       .UseTypoCorrections()
                       .UseParseErrorReporting()
                       .UseExceptionHandler((e, ctx) => exc = e)
                       .CancelOnProcessTermination()
                       .Build();

                await parser.InvokeAsync(args, _renderer);
            }
            catch (Exception e)
            {
                exc = e;
            }

            if (exc != null)
            {
                return HandleError(exc);
            }

            await _renderer.WhenCompleteAsync();
            return 0;
        }

        private int HandleError(Exception exc)
        {
            using (_renderer.WithColor(ConsoleColor.Red))
            {
                if (exc is PartyException partyExc)
                {
                    _renderer.Error.WriteLine(partyExc.Message);
                    return partyExc.Code;
                }

                if (exc is UnauthorizedAccessException unauthorizedExc)
                {
                    _renderer.Error.WriteLine(unauthorizedExc.Message);
                    return 2;
                }
            }

            ExceptionDispatchInfo.Capture(exc).Throw();
            return 1;
        }
    }
}
