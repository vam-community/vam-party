using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Party.Shared.Commands;

namespace Party.CLI.Commands
{
    public class PackageHandler : HandlerBase
    {
        [Verb("package", HelpText = "Provides a ready to use JSON for your scripts")]
        public class Options : HandlerBase.CommonOptions
        {
            [Value(0, MetaName = "script", Required = true, HelpText = "The path to the script, or script folder")]
            public string Script { get; set; }
        }

        public PackageHandler(PartyConfiguration config, TextWriter output) : base(config, output)
        {
        }

        public async Task<int> ExecuteAsync(Options opts)
        {
            var command = new PackageCommand(GetConfig(opts, Config));

            var result = await command.ExecuteAsync(Path.GetFullPath(opts.Script)).ConfigureAwait(false);

            Output.WriteLine("JSON Template:");
            await Output.WriteLineAsync(result.Formatted);

            return 0;
        }
    }
}
