using System;
using System.Collections.Generic;

namespace Snap.Nicole.UI.Xaml.Helpers;

internal static class MarkdownBlockPartitioner
{
    public static int GetStablePrefixLength(string markdown)
    {
        return GetStablePrefixLength(markdown, 0);
    }

    public static int GetStablePrefixLength(string markdown, int startIndex)
    {
        if (startIndex < 0 || startIndex > markdown.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (markdown.Length == 0)
        {
            return 0;
        }

        List<MarkdownLine> lines = GetCompleteLines(markdown, startIndex);
        int stableEnd = startIndex;
        bool inCodeBlock = false;

        for (int i = 0; i < lines.Count; i++)
        {
            MarkdownLine line = lines[i];

            if (IsCodeFenceLine(markdown, line))
            {
                if (inCodeBlock)
                {
                    inCodeBlock = false;
                    stableEnd = line.End;
                }
                else
                {
                    inCodeBlock = true;
                }

                continue;
            }

            if (inCodeBlock)
            {
                continue;
            }

            if (IsHeadingLine(markdown, line)
                && line.Start > startIndex
                && HasNonWhiteSpace(markdown, startIndex, line.Start))
            {
                return line.Start;
            }

            if (IsTableLine(markdown, line))
            {
                if (i + 1 >= lines.Count)
                {
                    break;
                }

                MarkdownLine nextLine = lines[i + 1];
                if (IsTableSeparator(markdown, nextLine))
                {
                    int tableEndLineIndex = i + 1;
                    int nextIndex = i + 2;
                    while (nextIndex < lines.Count && IsTableLine(markdown, lines[nextIndex]))
                    {
                        tableEndLineIndex = nextIndex;
                        nextIndex++;
                    }

                    if (nextIndex >= lines.Count)
                    {
                        break;
                    }

                    stableEnd = lines[tableEndLineIndex].End;
                    i = tableEndLineIndex;
                    continue;
                }
            }

            stableEnd = line.End;
        }

        return stableEnd;
    }

    private static List<MarkdownLine> GetCompleteLines(string markdown, int startIndex)
    {
        List<MarkdownLine> lines = [];
        int lineStart = startIndex;

        for (int i = startIndex; i < markdown.Length; i++)
        {
            char current = markdown[i];
            if (current is not '\r' and not '\n')
            {
                continue;
            }

            int contentEnd = i;
            int lineEnd = i + 1;
            if (current == '\r' && lineEnd < markdown.Length && markdown[lineEnd] == '\n')
            {
                lineEnd++;
            }

            lines.Add(new MarkdownLine(lineStart, contentEnd, lineEnd));
            lineStart = lineEnd;
            i = lineEnd - 1;
        }

        return lines;
    }

    public static bool StartsWithHeading(string markdown)
    {
        if (markdown.Length == 0)
        {
            return false;
        }

        List<MarkdownLine> lines = GetCompleteLines(markdown, 0);
        foreach (MarkdownLine line in lines)
        {
            if (HasNonWhiteSpace(markdown, line.Start, line.ContentEnd))
            {
                return IsHeadingLine(markdown, line);
            }
        }

        return false;
    }

    private static bool IsCodeFenceLine(string markdown, MarkdownLine line)
    {
        int start = line.Start;
        while (start < line.ContentEnd && char.IsWhiteSpace(markdown[start]))
        {
            start++;
        }

        return HasPrefix(markdown, start, line.ContentEnd, "```");
    }

    private static bool IsHeadingLine(string markdown, MarkdownLine line)
    {
        int start = line.Start;
        while (start < line.ContentEnd && char.IsWhiteSpace(markdown[start]))
        {
            start++;
        }

        int level = 0;
        while (start + level < line.ContentEnd && markdown[start + level] == '#')
        {
            level++;
        }

        return level is >= 1 and <= 7
            && start + level < line.ContentEnd
            && char.IsWhiteSpace(markdown[start + level]);
    }

    private static bool IsTableLine(string markdown, MarkdownLine line)
    {
        if (!TryGetTrimmedBounds(markdown, line, out int start, out int end))
        {
            return false;
        }

        return end - start > 2
            && markdown[start] == '|'
            && markdown[end - 1] == '|';
    }

    private static bool IsTableSeparator(string markdown, MarkdownLine line)
    {
        if (!TryGetTrimmedBounds(markdown, line, out int start, out int end)
            || end - start <= 2
            || markdown[start] != '|'
            || markdown[end - 1] != '|')
        {
            return false;
        }

        for (int i = start + 1; i < end - 1; i++)
        {
            if (markdown[i] is not '|' and not '-' and not ':' && !char.IsWhiteSpace(markdown[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasNonWhiteSpace(string markdown, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(markdown[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetTrimmedBounds(string markdown, MarkdownLine line, out int start, out int end)
    {
        start = line.Start;
        end = line.ContentEnd;

        while (start < end && char.IsWhiteSpace(markdown[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(markdown[end - 1]))
        {
            end--;
        }

        return start < end;
    }

    private static bool HasPrefix(string markdown, int start, int end, string prefix)
    {
        if (end - start < prefix.Length)
        {
            return false;
        }

        for (int i = 0; i < prefix.Length; i++)
        {
            if (markdown[start + i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }

    private sealed class MarkdownLine
    {
        public MarkdownLine(int start, int contentEnd, int end)
        {
            Start = start;
            ContentEnd = contentEnd;
            End = end;
        }

        public int Start { get; }

        public int ContentEnd { get; }

        public int End { get; }
    }
}
