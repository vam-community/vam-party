using System.Threading.Tasks;
using Party.Shared.Discovery;

namespace Party.Shared.Commands
{
    public abstract class HandlerBase
    {
        protected PartyConfiguration Config;

        public HandlerBase(PartyConfiguration config)
        {
            Config = config;
        }

        protected Task<SavesMap> ScanLocalScripts()
        {
            return SavesResolver.Resolve(SavesScanner.Scan(Config.VirtAMate.SavesDirectory, Config.Scanning.Ignore));
        }
    }
}
