using Microsoft.Extensions.Primitives;

namespace Snap.Nicole.Core.Primitives;

internal static class StringSegmentExtensions
{
    extension(StringSegment segment)
    {
        public bool IsWhiteSpace()
        {
            return segment.AsSpan().IsWhiteSpace();
        }
    }
}
