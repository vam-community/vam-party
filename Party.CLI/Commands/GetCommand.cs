using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;

namespace Party.CLI.Commands
{
    public class GetCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer output, PartyConfiguration config, PartyController controller)
        {
            var command = new Command("get", "Downloads a package (script) into the saves folder");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null) { Arity = ArgumentArity.ExactlyOne });

            command.Handler = CommandHandler.Create(async (string saves, string package) =>
            {
                await new GetCommand(output, config, saves, controller).ExecuteAsync(package);
            });
            return command;
        }

        public GetCommand(IRenderer output, PartyConfiguration config, string saves, PartyController controller) : base(output, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string package)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                throw new UserInputException("You must specify a package");
            }

            var registry = await Controller.GetRegistryAsync().ConfigureAwait(false);

            var registryPackage = registry.Scripts.FirstOrDefault(s => s.Name.Equals(package, StringComparison.InvariantCultureIgnoreCase));

            // TODO: Throw better exceptions
            if (registryPackage == null)
            {
                throw new RegistryException($"Package not found: '{package}'");
            }

            throw new NotImplementedException();
        }
    }
}
