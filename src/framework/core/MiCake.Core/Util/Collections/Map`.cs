﻿namespace MiCake.Core.Util.Collections
{
    /// <summary>
    /// A bidirectional key value pair mapping class.
    /// You can get a value from a key, or get a key from a value.
    /// 
    /// <para>
    ///     map.Forward[key]
    ///     map.Reverse[value]
    /// </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    internal class Map<T1, T2> where T1 : notnull where T2 : notnull
    {
        private readonly Dictionary<T1, T2> _forward = new();
        private readonly Dictionary<T2, T1> _reverse = new();

        public Map()
        {
            Forward = new Indexer<T1, T2>(_forward);
            Reverse = new Indexer<T2, T1>(_reverse);
        }

        public void Add(T1 t1, T2 t2)
        {
            _forward.Add(t1, t2);
            _reverse.Add(t2, t1);
        }

        public void Clear()
        {
            _forward.Clear();
            _reverse.Clear();
        }

        public Indexer<T1, T2> Forward { get; private set; }
        public Indexer<T2, T1> Reverse { get; private set; }

        public class Indexer<T3, T4> where T3 : notnull where T4 : notnull
        {
            private readonly Dictionary<T3, T4> _dictionary;
            public Indexer(Dictionary<T3, T4> dictionary)
            {
                _dictionary = dictionary;
            }

            public T4 this[T3 index]
            {
                get { return _dictionary[index]; }
                set { _dictionary[index] = value; }
            }

            public bool TryGetValue(T3 key, out T4 value)
            {
                return _dictionary.TryGetValue(key, out value!);
            }
        }
    }
}
