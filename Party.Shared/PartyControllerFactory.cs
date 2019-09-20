namespace Party.Shared
{
    public class PartyControllerFactory : IPartyControllerFactory
    {
        public IPartyController Create(PartyConfiguration config)
        {
            return new PartyController(config);
        }
    }

    public interface IPartyControllerFactory
    {
        IPartyController Create(PartyConfiguration config);
    }
}
