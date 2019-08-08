namespace Party.Shared.Commands
{
    public abstract class CommandBase
    {
        protected PartyConfiguration _config;

        public CommandBase(PartyConfiguration config)
        {
            _config = config;
        }
    }
}
