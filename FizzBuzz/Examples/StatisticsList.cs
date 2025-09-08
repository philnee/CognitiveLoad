using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepModuleExample
{
    // A deep module: encapsulates a concept (statistics on a list of ints),
    // hides implementation, and provides a simple, powerful interface.
    public class StatisticsList
    {
        private readonly List<int> _data = new List<int>();

        public void Add(int value) => _data.Add(value);
        public bool Remove(int value) => _data.Remove(value);
        public void Clear() => _data.Clear();
        public int Count => _data.Count;

        public double GetMean()
        {
            if (_data.Count == 0) throw new InvalidOperationException("No elements");
            return _data.Average();
        }

        public double GetMedian()
        {
            if (_data.Count == 0) throw new InvalidOperationException("No elements");
            var sorted = _data.OrderBy(x => x).ToList();
            int n = sorted.Count;
            if (n % 2 == 1)
                return sorted[n / 2];
            return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }

        public IEnumerable<int> GetMode()
        {
            if (_data.Count == 0) throw new InvalidOperationException("No elements");
            var groups = _data.GroupBy(x => x).Select(g => new { Value = g.Key, Count = g.Count() });
            int maxCount = groups.Max(g => g.Count);
            return groups.Where(g => g.Count == maxCount).Select(g => g.Value);
        }

        public int GetMin()
        {
            if (_data.Count == 0) throw new InvalidOperationException("No elements");
            return _data.Min();
        }

        public int GetMax()
        {
            if (_data.Count == 0) throw new InvalidOperationException("No elements");
            return _data.Max();
        }

        public int GetRange()
        {
            if (_data.Count == 0) throw new InvalidOperationException("No elements");
            return GetMax() - GetMin();
        }
    }
}

