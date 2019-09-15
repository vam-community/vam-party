namespace Party.Shared.Utils
{
    public class ProgressReporterMock<T> : IProgressReporter<T>
    {
        public void Notify(T item)
        {
        }
    }
}
