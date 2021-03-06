﻿using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
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
                .AddJsonFile("party.settings.dev.json", optional: true, reloadOnChange: false)
                .Build();

            var renderer = new ConsoleRenderer(Console.Out, Console.In, Console.Error, (ConsoleColor color) => Console.ForegroundColor = color, () => Console.ResetColor());

            string vamFolder = FindVaMDirectory();

            var config = PartyConfigurationFactory.Create(vamFolder);
            rootConfig.Bind(config);

            var controllerFactory = new PartyControllerFactory();

            return await new Program(renderer, config, controllerFactory).Execute(args).ConfigureAwait(false);
        }

        private static string FindVaMDirectory()
        {
            var vamFolder = AppContext.BaseDirectory;
            if (vamFolder == null) return null;

            while (!File.Exists(Path.Combine(vamFolder, "VaM.exe")))
            {
                var parent = Path.GetDirectoryName(vamFolder);
                if (parent == null)
                    return AppContext.BaseDirectory;
                if (parent == vamFolder)
                    return AppContext.BaseDirectory;
                vamFolder = parent;
            }

            return vamFolder;
        }

        private readonly IConsoleRenderer _renderer;
        private readonly PartyConfiguration _config;
        private readonly IPartyControllerFactory _controllerFactory;

        public Program(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            _renderer = renderer;
            _config = config;
            _controllerFactory = controllerFactory;
        }

        public async Task<int> Execute(string[] args)
        {
            var rootCommand = new RootCommand("Party: A Virt-A-Mate package manager")
            {
                HelpCommand.CreateCommand(_renderer, _config, _controllerFactory),
                SearchCommand.CreateCommand(_renderer, _config, _controllerFactory),
                GetCommand.CreateCommand(_renderer, _config, _controllerFactory),
                ShowCommand.CreateCommand(_renderer, _config, _controllerFactory),
                StatusCommand.CreateCommand(_renderer, _config, _controllerFactory),
                UpgradeCommand.CreateCommand(_renderer, _config, _controllerFactory),
                PublishCommand.CreateCommand(_renderer, _config, _controllerFactory),
                CleanCommand.CreateCommand(_renderer, _config, _controllerFactory),
            };

            // For CoreRT:
            rootCommand.Name = Path.GetFileName(Environment.GetCommandLineArgs().FirstOrDefault()) ?? "party.exe";

            Exception exc = null;
            var parser = new CommandLineBuilder(rootCommand)
                .UseVersionOption()
                .UseHelp()
#if DEBUG
                .UseParseDirective()
                .UseDebugDirective()
#endif
                    .UseSuggestDirective()
                // .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler((e, ctx) => exc = e)
                .CancelOnProcessTermination()
                .Build();

            _renderer.WriteLine("Party, a Virt-A-Mate package manager, is still in it's early stages. Please file any issue or ideas at https://github.com/vam-community/vam-party/issues", ConsoleColor.Green);

            try
            {
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

            return 0;
        }

        private int HandleError(Exception exc)
        {
            // Since we might get an error while a line is being written:
            _renderer.Error.WriteLine();

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

                _renderer.Error.WriteLine(
                    string.Join(
                        Environment.NewLine,
                        exc.ToString()
                            .Split(new[] { '\r', '\n' })
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .Where(line => line != "--- End of stack trace from previous location where exception was thrown ---")
                            .Where(line => !line.StartsWith("   at System.CommandLine."))
                            .Where(line => !line.StartsWith("   at System.Runtime."))));
                return 1;
            }
        }
    }
}
