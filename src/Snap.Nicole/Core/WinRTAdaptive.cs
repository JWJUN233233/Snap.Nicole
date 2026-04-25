using System.Collections.Frozen;
using System.Collections.Generic;

namespace Snap.Nicole.Core;

internal static class WinRTAdaptive
{
    public static FrozenDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] source)
        where TKey : notnull
    {
        return source.ToFrozenDictionary();
    }

    public static T[] Array<T>(T[] source)
    {
        return source;
    }
}