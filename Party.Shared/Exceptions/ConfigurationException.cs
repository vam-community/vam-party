namespace Party.Shared.Exceptions
{
    public class ConfigurationException : PartyException
    {
        public override int Code => 200;

        public ConfigurationException(string message) : base(message)
        {
        }
    }
}
