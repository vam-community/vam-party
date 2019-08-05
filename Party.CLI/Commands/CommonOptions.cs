using CommandLine;

namespace Party.CLI.Commands
{
    public class CommonOptions
    {
        [Option('p', "path", Required = false, HelpText = "The Saves directory; defaults to the Saves folder under the current directory.")]
        public string Saves { get; set; }
    }
}
