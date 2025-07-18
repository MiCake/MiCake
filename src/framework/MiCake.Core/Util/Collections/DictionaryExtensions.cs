﻿using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Util.Collections
{
    public static class DictionaryExtensions
    {
        public static bool HasValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value)
        {
            return dic.Values.Any(v => v.Equals(value));
        }

        public static List<TKey> GetKeyByValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value)
        {
            return [.. dic.Where(v => v.Equals(value)).Select(k => k.Key)];
        }

        public static TKey GetFirstKeyByValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TValue value)
        {
            return dic.Where(v => v.Equals(value)).Select(k => k.Key).FirstOrDefault();
        }
    }
}
