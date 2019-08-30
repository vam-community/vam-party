using System;

namespace Party.Shared.Exceptions
{
    public abstract class PartyException : Exception
    {
        public virtual int Code => 10;

        protected PartyException(string message) : base(message)
        {
        }

        protected PartyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
