using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Results;

namespace Party.CLI.Commands
{
    public class PublishCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("publish", "Prepares files for publishing");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("input", null));
            // TODO: Add the different fields too (author, name, etc.)

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string input) =>
            {
                await new PublishCommand(renderer, config, saves, controller).ExecuteAsync(input);
            });
            return command;
        }

        public PublishCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string input)
        {
            var registry = await Controller.GetRegistryAsync();
            var name = await Renderer.AskAsync("Package Name: ");
            // TODO: Validate
            var script = registry.Scripts?.FirstOrDefault(s => s.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) ?? false);
            if (script != null)
            {
                Renderer.WriteLine($"This package already exists (by {script.Author?.Name ?? "Anonymous User"})");
                if (script.Versions != null)
                {
                    Renderer.WriteLine("Existing versions:");
                    foreach (var existingVersion in script.Versions)
                    {
                        Renderer.WriteLine($"- {existingVersion.Version}");
                    }
                }
            }
            else
            {
                Renderer.WriteLine("Looks like a new package in the registry! Please provide some information about this new package, or press CTRL+C if you want to abort.");
                script = new RegistryScript
                {
                    Name = name,
                    Author = new RegistryScriptAuthor
                    {
                        Name = await Renderer.AskAsync("Author Name: "),
                        Profile = await Renderer.AskAsync("Author Profile URL: ")
                    },
                    Description = "",
                    Tags = (await Renderer.AskAsync("Tags (comma-separated list): ")).Split(',').Select(x => x.Trim()).Where(x => x != "").ToList(),
                    Homepage = await Renderer.AskAsync("Package Homepage URL: "),
                    Repository = await Renderer.AskAsync("Package Repository URL: ")
                };
            }
            var version = new RegistryScriptVersion
            {
                Version = await Renderer.AskAsync("Package Version (0.0.0): ")
            };
            var result = await Controller.Publish(script, version, input).ConfigureAwait(false);

            // TODO: Instead, write directly to the JSON file in a specified path
            Renderer.WriteLine("JSON Template:");
            Renderer.WriteLine(result.Formatted);
        }
    }
}
