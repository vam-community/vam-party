using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Party.Shared.Utils
{
    public class ProgressReporterTests
    {
        [Test]
        public void ReportsProgress()
        {
            var items = new List<int>();
            void Start()
            {
                items.Add(0);
            }
            void Progress(int item)
            {
                items.Add(item);
            }
            void End()
            {
                items.Add(4);
            }
            using (var reporter = new ProgressReporter<int>(Start, Progress, End))
            {
                reporter.Report(1);
                Thread.Sleep(1);
                reporter.Report(2);
                Thread.Sleep(1);
                reporter.Report(3);
                Thread.Sleep(1);
            }

            Assert.That(items.FirstOrDefault(), Is.EqualTo(0));
            Assert.That(items.LastOrDefault(), Is.EqualTo(4));
            var subset = items.Where(v => v != 0 && v != 4).ToArray();
            CollectionAssert.IsNotEmpty(subset);
            CollectionAssert.IsSubsetOf(subset, new[] { 1, 2, 3 });
        }
    }
}
