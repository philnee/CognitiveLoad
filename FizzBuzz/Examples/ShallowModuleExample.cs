// Classic shallow module example: a wrapper that exposes almost nothing beyond what the underlying type already does.
// In this case, a wrapper around List<T> that adds no real abstraction or value.
using System;
using System.Collections.Generic;

namespace ShallowModuleExample
{
    // This class is a shallow module: it exposes almost the same interface as List<T>,
    // providing no meaningful abstraction or simplification.
    public class IntListWrapper
    {
        private readonly List<int> _list = new List<int>();

        public void Add(int value) => _list.Add(value);
        public void Remove(int value) => _list.Remove(value);
        public int this[int index] { get => _list[index]; set => _list[index] = value; }
        public int Count => _list.Count;
        public void Clear() => _list.Clear();
        public bool Contains(int value) => _list.Contains(value);
        public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();
        // ...and so on, just forwarding to List<int>...
    }
}

