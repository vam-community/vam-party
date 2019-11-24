using Party.Shared.Logging;

namespace Party.Shared
{
    public class PartyControllerFactory : IPartyControllerFactory
    {
        public IPartyController Create(PartyConfiguration config, ILogger logger, bool checksEnabled)
        {
            return new PartyController(config, logger, checksEnabled);
        }
    }

    public interface IPartyControllerFactory
    {
        IPartyController Create(PartyConfiguration config, ILogger logger, bool checksEnabled);
    }
}
