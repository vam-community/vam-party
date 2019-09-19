using System.CommandLine;

namespace Party.CLI
{
    public static class OptionExtensions
    {
        public static Option WithAlias(this Option o, string alias)
        {
            o.AddAlias(alias);
            return o;
        }
    }
}