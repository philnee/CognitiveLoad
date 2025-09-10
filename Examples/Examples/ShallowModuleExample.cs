using System;
using System.Collections.Generic;

namespace ShallowModuleExample
{
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
    }
}

