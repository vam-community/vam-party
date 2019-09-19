using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Local;

namespace Party.CLI.Commands
{
    public class UpgradeCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("upgrade", "Updates scenes to reference scripts from the Party folder. You can specify a package, scene or script to upgrade. If you don't specify anything, all scenes and scripts will be upgraded.");
            AddCommonOptions(command);
            // TODO: Specify specific scenes and/or specific scripts and/or specific packages to upgrade
            command.AddArgument(new Argument<string>("filter") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option("--all", "Upgrade everything"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene"));

            command.Handler = CommandHandler.Create<UpgradeArguments>(async args =>
            {
                await new UpgradeCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class UpgradeArguments : CommonArguments
        {
            public string Filter { get; set; }
            public bool All { get; set; }
            public bool Warnings { get; set; }
            public bool Noop { get; set; }
            public bool Verbose { get; set; }
        }

        public UpgradeCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(UpgradeArguments args)
        {
            Controller.HealthCheck();

            if (args.All && args.Filter != null)
                throw new UserInputException("You cannot specify --all and an item to upgrade at the same time");
            else if (!args.All && args.Filter == null)
                throw new UserInputException("You must specify what to upgrade (a .cs, .cslist, .json or package name), or pass --all to upgrade everything");

            var (saves, registry) = await GetSavesAndRegistryAsync(args.Filter);

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            PrintWarnings(args.Warnings, saves);

            foreach (var match in matches.HashMatches)
            {
                await HandleOne(match, args);
            }
        }

        private async Task HandleOne(RegistrySavesMatch match, UpgradeArguments args)
        {
            var latestVersion = match.Remote.Package.GetLatestVersion();
            var updateToVersion = latestVersion.Version.Equals(match.Remote.Version.Version) ? null : latestVersion;

            if (updateToVersion == null)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null);
                    PrintWarnings(args.Warnings, match.Local);
                    Renderer.WriteLine($"  Skipping because no updates are available", ConsoleColor.DarkGray);
                }
                return;
            }

            PrintScriptToPackage(match, updateToVersion);
            PrintWarnings(args.Warnings, match.Local);

            var info = await Controller.GetInstalledPackageInfoAsync(match.Remote.WithVersion(updateToVersion ?? match.Remote.Version));

            if (info.Installed)
            {
                Renderer.WriteLine("  Already installed");
                return;
            }

            if (!args.Force && (info.Corrupted || !info.Installable))
            {
                Renderer.WriteLine("  Cannot upgrade because at least one file is either broken or not downloadable.");
                Renderer.WriteLine($"  You can instead download it at {match.Remote.Version.DownloadUrl ?? match.Remote.Package.Homepage ?? match.Remote.Package.Repository ?? "(no link provided)"}");
                Renderer.WriteLine("  Files:");
                PrintInstalledFiles(info, "  ");
                if (!args.Force)
                    return;
            }

            if (args.Noop)
            {
                Renderer.WriteLine("  Skipping install because the --noop option was specified", ConsoleColor.Yellow);
            }
            else
            {
                Renderer.Write($"  Downloading...");
                info = await Controller.InstallPackageAsync(info, args.Force);
                Renderer.WriteLine($"  installed in {info.PackageFolder}:", ConsoleColor.Green);
                PrintInstalledFiles(info);
            }
        }
    }
}
