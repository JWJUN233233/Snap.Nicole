using System.Collections.Generic;

namespace Snap.Nicole.Core;

internal static class Enumerable
{
    public static IEnumerable<T> Enumerate<T>(params IEnumerable<T> values)
    {
        return values;
    }
}