using System.Buffers;
using Snap.Nicole.Core;

namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

internal static class MarkdownBlockPartitioner
{
    private static readonly SearchValues<char> LineEndings = SearchValues.Create("\r\f\u0085\u2028\u2029\n");

    public static int GetStablePrefixLength(string markdown)
    {
        return GetStablePrefixLength(markdown, 0);
    }

    public static int GetStablePrefixLength(ReadOnlySpan<char> markdown, int startIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, markdown.Length);

        if (markdown.Length is 0)
        {
            return 0;
        }

        int stableEnd = startIndex;
        bool inCodeBlock = false;
        int codeFenceLength = 0;
        int nextLineStart = startIndex;
        MarkdownLine? pendingLine = null;

        while (TryReadCompleteLine(markdown, ref nextLineStart, ref pendingLine, out MarkdownLine line))
        {
            if (!inCodeBlock && TryGetCodeFenceLine(markdown, line, /* At least ``` */ 3, out CodeFenceLine openingFenceLine))
            {
                inCodeBlock = true;
                codeFenceLength = openingFenceLine.FenceLength;

                continue;
            }

            if (inCodeBlock)
            {
                if (TryGetCodeFenceLine(markdown, line, codeFenceLength, out CodeFenceLine closingFenceLine)
                    && markdown[closingFenceLine.FenceInfoStart..line.ContentEnd].IsWhiteSpace())
                {
                    inCodeBlock = false;
                    codeFenceLength = 0;
                    stableEnd = line.End;
                    continue;
                }

                continue;
            }

            if (IsHeadingLine(markdown, line) && line.Start > startIndex && !markdown[startIndex..line.Start].IsWhiteSpace())
            {
                return line.Start;
            }

            if (IsTableLine(markdown, line))
            {
                if (!TryReadCompleteLine(markdown, ref nextLineStart, ref pendingLine, out MarkdownLine nextLine))
                {
                    break;
                }

                if (IsTableSeparator(markdown, nextLine))
                {
                    MarkdownLine tableEndLine = nextLine;
                    while (TryReadCompleteLine(markdown, ref nextLineStart, ref pendingLine, out MarkdownLine tableLine))
                    {
                        if (!IsTableLine(markdown, tableLine))
                        {
                            pendingLine = tableLine;
                            break;
                        }

                        tableEndLine = tableLine;
                    }

                    if (!pendingLine.HasValue)
                    {
                        break;
                    }

                    stableEnd = tableEndLine.End;
                    continue;
                }

                pendingLine = nextLine;
            }

            stableEnd = line.End;
        }

        return stableEnd;
    }

    private static bool TryReadCompleteLine(ReadOnlySpan<char> markdown, ref int lineStart, ref MarkdownLine? pendingLine, out MarkdownLine line)
    {
        if (pendingLine.HasValue)
        {
            line = pendingLine.GetValueOrDefault();
            pendingLine = null;
            return true;
        }

        return TryReadCompleteLine(markdown, ref lineStart, out line);
    }

    private static bool TryReadCompleteLine(ReadOnlySpan<char> markdown, ref int lineStart, out MarkdownLine line)
    {
        int offset = markdown[lineStart..].IndexOfAny(LineEndings);
        if (offset < 0)
        {
            line = default;
            return false;
        }

        int contentEnd = lineStart + offset;
        int lineEnd = contentEnd + 1;

        if (markdown[contentEnd] is '\r' && lineEnd < markdown.Length && markdown[lineEnd] is '\n')
        {
            lineEnd++;
        }

        line = new(lineStart, contentEnd, lineEnd);
        lineStart = lineEnd;
        return true;
    }

    public static bool StartsWithHeading(ReadOnlySpan<char> markdown)
    {
        if (markdown.Length is 0)
        {
            return false;
        }

        int nextLineStart = 0;
        while (TryReadCompleteLine(markdown, ref nextLineStart, out MarkdownLine line))
        {
            if (!line.GetContent(markdown).IsWhiteSpace())
            {
                return IsHeadingLine(markdown, line);
            }
        }

        return false;
    }

    private static bool TryGetCodeFenceLine(ReadOnlySpan<char> markdown, MarkdownLine line, int minimumFenceLength, out CodeFenceLine codeFenceLine)
    {
        int fenceLength = 0;
        ReadOnlySpan<char> remaining = line.GetContent(markdown).TrimStart(out int start);

        while (fenceLength < remaining.Length && remaining[fenceLength] is '`')
        {
            fenceLength++;
        }

        if (fenceLength < minimumFenceLength)
        {
            codeFenceLine = default;
            return false;
        }

        codeFenceLine = new(fenceLength, line.Start + start + fenceLength);
        return true;
    }

    private static bool IsHeadingLine(ReadOnlySpan<char> markdown, MarkdownLine line)
    {
        ReadOnlySpan<char> content = line.GetContent(markdown).TrimStart(out _);
        int level = content.IndexOfAnyExcept('#');

        return level is >= 1 and <= 6
            && char.IsWhiteSpace(content[level]);
    }

    private static bool IsTableLine(ReadOnlySpan<char> markdown, MarkdownLine line)
    {
        if (!TryGetTrimmedBounds(markdown, line, out TrimmedBounds bounds))
        {
            return false;
        }

        return bounds.Length > 2
            && markdown[bounds.Start] is '|'
            && markdown[bounds.LastIndex] is '|'
            && !IsEscaped(markdown, bounds.LastIndex);
    }

    private static bool IsTableSeparator(ReadOnlySpan<char> markdown, MarkdownLine line)
    {
        if (!TryGetTrimmedBounds(markdown, line, out TrimmedBounds bounds)
            || bounds.Length <= 2
            || markdown[bounds.Start] is not '|'
            || markdown[bounds.LastIndex] is not '|')
        {
            return false;
        }

        for (int i = bounds.Start + 1; i < bounds.LastIndex; i++)
        {
            if (markdown[i] is not ('|' or '-' or ':') && !char.IsWhiteSpace(markdown[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetTrimmedBounds(ReadOnlySpan<char> markdown, MarkdownLine line, out TrimmedBounds bounds)
    {
        ReadOnlySpan<char> content = line.GetContent(markdown);
        content.TrimStart(out int startOffset);
        content.TrimEnd(out int endOffset);

        if (startOffset > endOffset)
        {
            bounds = default;
            return false;
        }

        int start = line.Start + startOffset;
        int end = line.Start + endOffset + 1;
        bounds = new(start, end);
        return true;
    }

    private static bool IsEscaped(ReadOnlySpan<char> markdown, int index)
    {
        int lastNonEscape = markdown[..index].LastIndexOfAnyExcept('\\');
        int backslashCount = index - lastNonEscape - 1;
        return backslashCount % 2 != 0;
    }

    private readonly struct CodeFenceLine(int fenceLength, int fenceInfoStart)
    {
        public int FenceLength { get; } = fenceLength;

        public int FenceInfoStart { get; } = fenceInfoStart;
    }

    private readonly struct TrimmedBounds(int start, int end)
    {
        public int Start { get; } = start;

        public int End { get; } = end;

        public int Length { get => End - Start; }

        public int LastIndex { get => End - 1; }
    }

    private readonly struct MarkdownLine(int start, int contentEnd, int end)
    {
        public int Start { get; } = start;

        public int ContentEnd { get; } = contentEnd;

        public int End { get; } = end;

        public ReadOnlySpan<char> GetContent(ReadOnlySpan<char> markdown)
        {
            return markdown[Start..ContentEnd];
        }
    }
}
