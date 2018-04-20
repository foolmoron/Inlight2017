using System;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class DictionaryExtensions {
    public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) {
        TValue val;
        return dict.TryGetValue(key, out val) ? val : default(TValue);
    }
}