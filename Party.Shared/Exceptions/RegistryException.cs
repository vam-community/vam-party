namespace Party.Shared.Exceptions
{
    public class RegistryException : PartyException
    {
        public override int Code => 300;

        public RegistryException(string message) : base(message)
        {

        }
    }
}
