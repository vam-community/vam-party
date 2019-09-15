using System;

namespace Party.Shared.Utils
{
    public class ProgressMock<T> : IProgress<T>
    {
        public void Report(T item)
        {
        }
    }
}
