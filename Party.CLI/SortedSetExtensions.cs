using System.Collections.Generic;

namespace Party.Shared
{
    public static class SortedSetExtensions
    {
        public static void AddRange<T>(this SortedSet<T> set, IEnumerable<T> range)
        {
            foreach (var item in range)
                set.Add(item);
        }
    }
}
