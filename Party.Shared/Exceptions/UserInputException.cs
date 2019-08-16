namespace Party.Shared.Exceptions
{
    public class UserInputException : PartyException
    {
        public override int Code => 100;

        public UserInputException(string message) : base(message)
        {

        }
    }
}
