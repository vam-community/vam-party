using System.Threading.Tasks;
using Party.Shared.Discovery;

namespace Party.Shared.Commands
{
    public abstract class CommandBase
    {
        protected PartyConfiguration Config;

        public CommandBase(PartyConfiguration config)
        {
            Config = config;
        }

        protected Task<SavesMap> ScanLocalScripts()
        {
            return SavesResolver.Resolve(SavesScanner.Scan(Config.VirtAMate.SavesDirectory, Config.Scanning.Ignore));
        }
    }
}
