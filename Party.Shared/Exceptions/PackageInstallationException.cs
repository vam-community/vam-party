namespace Party.Shared.Exceptions
{
    public class PackageInstallationException : PartyException
    {
        public override int Code => 400;

        public PackageInstallationException(string message)
            : base(message)
        {
        }
    }
}
