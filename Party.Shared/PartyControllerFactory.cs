namespace Party.Shared
{
    public class PartyControllerFactory : IPartyControllerFactory
    {
        public IPartyController Create(PartyConfiguration config, bool checksEnabled)
        {
            return new PartyController(config, checksEnabled);
        }
    }

    public interface IPartyControllerFactory
    {
        IPartyController Create(PartyConfiguration config, bool checksEnabled);
    }
}
