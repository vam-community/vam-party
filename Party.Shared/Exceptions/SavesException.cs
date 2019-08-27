namespace Party.Shared.Exceptions
{
    public class SavesException : PartyException
    {
        public override int Code => 500;

        public SavesException(string message) : base(message)
        {
        }
    }
}
