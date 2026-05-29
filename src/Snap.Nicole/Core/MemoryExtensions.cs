namespace Snap.Nicole.Core;

internal static class MemoryExtensions
{
    extension(ReadOnlySpan<char> span)
    {
        public ReadOnlySpan<char> TrimStart(out int start)
        {
            start = 0;
            for (; start < span.Length; start++)
            {
                if (!char.IsWhiteSpace(span[start]))
                {
                    break;
                }
            }

            return span[start..];
        }

        public ReadOnlySpan<char> TrimEnd(out int end)
        {
            end = span.Length - 1;
            for (; end >= 0; end--)
            {
                if (!char.IsWhiteSpace(span[end]))
                {
                    break;
                }
            }

            return span[..(end + 1)];
        }
    }
}
