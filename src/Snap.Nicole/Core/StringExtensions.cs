using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Snap.Nicole.Core;

internal static class StringExtensions
{
    extension(string)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNullOrEmpty([NotNullWhen(true)] string? value)
        {
            return !string.IsNullOrEmpty(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNullOrWhiteSpace([NotNullWhen(true)] string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    extension(string? str)
    {
        public Uri? ToUri()
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            return new(str);
        }
    }
}
