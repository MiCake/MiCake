#nullable disable warnings

﻿using System.Collections.Generic;
using System.Linq;

namespace MiCake.Util.Collection
{
    public static class DictionaryExtensions
    {
        public static bool HasValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value)
        {
            return dic.Values.Any(v => EqualityComparer<TValue>.Default.Equals(v, value));
        }

        public static List<TKey> GetKeyByValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value)
        {
            return [.. dic.Where(kvp => EqualityComparer<TValue>.Default.Equals(kvp.Value, value)).Select(kvp => kvp.Key)];
        }

        public static TKey GetFirstKeyByValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value)
        {
            return dic.FirstOrDefault(kvp => EqualityComparer<TValue>.Default.Equals(kvp.Value, value)).Key;
        }
    }
}
