using Microsoft.Extensions.Primitives;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Snap.Nicole.UI.Xaml.Controls.ChatElements;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Snap.Nicole.UI.Xaml.Helpers;

internal static partial class MarkdownHelper
{
    private static readonly char[] LineFeedSeparators = ['\n'];
    private static readonly char[] CarriageReturnSeparators = ['\r'];
    private static readonly char[] TableCellSeparators = ['|'];

    public static RichTextBlock CreateMarkdownBlock(string? markdown)
    {
        RichTextBlock richText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14, // BodyTextBlockFontSize
            IsTextSelectionEnabled = true,
        };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return richText;
        }

        bool inCodeBlock = false;
        StringBuilder? codeBuffer = null;
        string codeLanguage = string.Empty;
        StringTokenizer tokenizer = new(markdown, GetLineSeparators(markdown));
        StringTokenizer.Enumerator enumerator = tokenizer.GetEnumerator();
        bool hasPendingLine = false;
        StringSegment pendingLine = default;

        while (TryReadLine(ref enumerator, ref hasPendingLine, ref pendingLine, out StringSegment line))
        {
            StringSegment trimmedStart = line.TrimStart();

            if (trimmedStart.StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    AddCodeBlock(richText, GetCodeBlockText(codeBuffer), codeLanguage);
                    codeBuffer?.Clear();
                    codeLanguage = string.Empty;
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                    codeLanguage = Slice(trimmedStart, 3).Trim().ToString();
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeBuffer ??= new StringBuilder();
                codeBuffer.Append(line.AsSpan());
                codeBuffer.Append('\n');
                continue;
            }

            if (IsWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                AddHeading(richText, Slice(line, 4), 18);
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AddHeading(richText, Slice(line, 3), 20);
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                AddHeading(richText, Slice(line, 2), 24);
            }
            else if (IsHorizontalRule(line))
            {
                AddHorizontalRule(richText);
            }
            else if (IsTableLine(line) && TryReadLine(ref enumerator, ref hasPendingLine, ref pendingLine, out StringSegment separatorLine))
            {
                if (IsTableSeparator(separatorLine))
                {
                    AddTable(richText, line, ref enumerator, ref hasPendingLine, ref pendingLine);
                }
                else
                {
                    pendingLine = separatorLine;
                    hasPendingLine = true;
                    AddParagraph(richText, line);
                }
            }
            else if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                AddBlockquote(richText, Slice(line, 2));
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                AddListItem(richText, Slice(line, 2), "- ");
            }
            else if (TryParseOrderedListItem(line, out StringSegment listNumber, out StringSegment listText))
            {
                AddListItem(richText, listText, CreateOrderedListBullet(listNumber));
            }
            else
            {
                AddParagraph(richText, line);
            }
        }

        if (inCodeBlock)
        {
            AddCodeBlock(richText, GetCodeBlockText(codeBuffer), codeLanguage);
        }

        return richText;
    }

    private static char[] GetLineSeparators(string markdown)
    {
        if (!markdown.Contains('\n') && markdown.Contains('\r'))
        {
            return CarriageReturnSeparators;
        }

        return LineFeedSeparators;
    }

    private static bool TryReadLine(
        ref StringTokenizer.Enumerator enumerator,
        ref bool hasPendingLine,
        ref StringSegment pendingLine,
        out StringSegment line)
    {
        if (hasPendingLine)
        {
            line = pendingLine;
            hasPendingLine = false;
            return true;
        }

        if (!enumerator.MoveNext())
        {
            line = default;
            return false;
        }

        line = TrimTrailingCarriageReturn(enumerator.Current);
        return true;
    }

    private static StringSegment TrimTrailingCarriageReturn(StringSegment line)
    {
        if (line.Length > 0 && line[line.Length - 1] == '\r')
        {
            return Slice(line, 0, line.Length - 1);
        }

        return line;
    }

    private static StringSegment Slice(StringSegment segment, int offset)
    {
        if (offset >= segment.Length)
        {
            return new StringSegment(segment.Buffer!, segment.Offset + segment.Length, 0);
        }

        return segment.Subsegment(offset);
    }

    private static StringSegment Slice(StringSegment segment, int offset, int length)
    {
        if (length <= 0)
        {
            return new StringSegment(segment.Buffer!, segment.Offset + offset, 0);
        }

        return segment.Subsegment(offset, length);
    }

    private static bool IsWhiteSpace(StringSegment text)
    {
        ReadOnlySpan<char> span = text.AsSpan();

        for (int i = 0; i < span.Length; i++)
        {
            if (!char.IsWhiteSpace(span[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetCodeBlockText(StringBuilder? codeBuffer)
    {
        if (codeBuffer is null || codeBuffer.Length == 0)
        {
            return string.Empty;
        }

        int length = codeBuffer.Length;
        while (length > 0 && (codeBuffer[length - 1] == '\r' || codeBuffer[length - 1] == '\n'))
        {
            length--;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        return codeBuffer.ToString(0, length);
    }

    private static bool IsHorizontalRule(StringSegment line)
    {
        ReadOnlySpan<char> span = line.Trim().AsSpan();

        if (span.Length < 3)
        {
            return false;
        }

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] is not '-' and not '*' and not '_')
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseOrderedListItem(StringSegment line, out StringSegment number, out StringSegment text)
    {
        ReadOnlySpan<char> span = line.AsSpan();
        int index = 0;

        while (index < span.Length && char.IsDigit(span[index]))
        {
            index++;
        }

        if (index == 0 || index + 1 >= span.Length || span[index] != '.' || !char.IsWhiteSpace(span[index + 1]))
        {
            number = default;
            text = default;
            return false;
        }

        number = Slice(line, 0, index);
        text = Slice(line, index + 2);
        return true;
    }

    private static string CreateOrderedListBullet(StringSegment number)
    {
        return string.Create(number.Length + 2, number, static (destination, segment) =>
        {
            segment.AsSpan().CopyTo(destination);
            destination[segment.Length] = '.';
            destination[segment.Length + 1] = ' ';
        });
    }

    private static void AddHeading(RichTextBlock richText, StringSegment text, double fontSize)
    {
        Paragraph paragraph = new() { FontSize = fontSize, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 2) };
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddHorizontalRule(RichTextBlock richText)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0, 4, 0, 4) };
        paragraph.Inlines.Add(new InlineUIContainer
        {
            Child = new Border
            {
                Height = 1,
                MinWidth = 120,
                Background = GetThemeBrush("CardStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            },
        });
        richText.Blocks.Add(paragraph);
    }

    private static void AddBlockquote(RichTextBlock richText, StringSegment text)
    {
        Paragraph paragraph = new()
        {
            Margin = new Thickness(12, 2, 0, 2),
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush", "SystemControlForegroundBaseMediumBrush"),
        };
        paragraph.Inlines.Add(new Run { Text = "> " });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddListItem(RichTextBlock richText, StringSegment text, string bullet)
    {
        Paragraph paragraph = new() { Margin = new Thickness(16, 1, 0, 1) };
        paragraph.Inlines.Add(new Run { Text = bullet });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddParagraph(RichTextBlock richText, StringSegment text)
    {
        Paragraph paragraph = new() { Margin = new Thickness(0, 2, 0, 2) };
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddCodeBlock(RichTextBlock richText, string code, string language)
    {
        Paragraph container = new();
        container.Inlines.Add(new InlineUIContainer
        {
            Child = new MarkdownCodeBlockView
            {
                Code = code,
                Language = language,
            },
        });
        richText.Blocks.Add(container);
    }

    private static bool IsTableLine(StringSegment line)
    {
        StringSegment trimmed = line.Trim();

        return trimmed.Length > 2
            && trimmed[0] == '|'
            && trimmed[trimmed.Length - 1] == '|';
    }

    private static bool IsTableSeparator(StringSegment line)
    {
        ReadOnlySpan<char> span = line.Trim().AsSpan();

        if (span.Length <= 2 || span[0] != '|' || span[span.Length - 1] != '|')
        {
            return false;
        }

        for (int i = 1; i < span.Length - 1; i++)
        {
            if (span[i] is not '|' and not '-' and not ':' && !char.IsWhiteSpace(span[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> ParseTableCells(StringSegment line)
    {
        StringSegment trimmed = TrimTableCellBounds(line);
        List<string> cells = [];
        StringTokenizer tokenizer = new(trimmed, TableCellSeparators);

        foreach (StringSegment cell in tokenizer)
        {
            cells.Add(cell.Trim().ToString());
        }

        return cells;
    }

    private static StringSegment TrimTableCellBounds(StringSegment line)
    {
        StringSegment trimmed = line.Trim();
        int start = 0;
        int length = trimmed.Length;

        while (length > 0 && trimmed[start] == '|')
        {
            start++;
            length--;
        }

        while (length > 0 && trimmed[start + length - 1] == '|')
        {
            length--;
        }

        return Slice(trimmed, start, length);
    }

    private static void AddTable(
        RichTextBlock richText,
        StringSegment headerLine,
        ref StringTokenizer.Enumerator enumerator,
        ref bool hasPendingLine,
        ref StringSegment pendingLine)
    {
        List<string> headerCells = ParseTableCells(headerLine);
        int columnCount = headerCells.Count;
        List<IReadOnlyList<string>> rows = [headerCells];

        while (TryReadLine(ref enumerator, ref hasPendingLine, ref pendingLine, out StringSegment line))
        {
            if (!IsTableLine(line))
            {
                pendingLine = line;
                hasPendingLine = true;
                break;
            }

            List<string> cells = ParseTableCells(line);
            while (cells.Count < columnCount)
            {
                cells.Add(string.Empty);
            }

            rows.Add(cells);
        }

        Paragraph container = new();
        container.Inlines.Add(new InlineUIContainer
        {
            Child = new MarkdownTableView
            {
                Rows = rows,
            },
        });
        richText.Blocks.Add(container);
    }

    private static void AddInlineMarkdown(InlineCollection inlines, StringSegment text)
    {
        if (text.Length == 0)
        {
            return;
        }

        AddInlineMarkdown(inlines, text.ToString());
    }

    internal static void AddInlineMarkdown(InlineCollection inlines, string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        Regex pattern = InlineMarkdownPattern();
        int lastIndex = 0;

        foreach (Match match in pattern.Matches(text))
        {
            if (match.Index > lastIndex)
            {
                inlines.Add(new Run { Text = text[lastIndex..match.Index] });
            }

            if (match.Groups["bolditalic"].Success)
            {
                Bold bold = new();
                bold.Inlines.Add(new Italic { Inlines = { new Run { Text = match.Groups["bolditalic"].Value } } });
                inlines.Add(bold);
            }
            else if (match.Groups["bold"].Success)
            {
                inlines.Add(new Bold { Inlines = { new Run { Text = match.Groups["bold"].Value } } });
            }
            else if (match.Groups["italic"].Success)
            {
                inlines.Add(new Italic { Inlines = { new Run { Text = match.Groups["italic"].Value } } });
            }
            else if (match.Groups["code"].Success)
            {
                inlines.Add(new InlineUIContainer
                {
                    Child = new MarkdownInlineCodeView
                    {
                        Text = match.Groups["code"].Value,
                    },
                });
            }
            else if (match.Groups["linktext"].Success)
            {
                try
                {
                    Hyperlink link = new();
                    link.Inlines.Add(new Run { Text = match.Groups["linktext"].Value });
                    link.NavigateUri = new Uri(match.Groups["linkurl"].Value);
                    inlines.Add(link);
                }
                catch
                {
                    inlines.Add(new Run { Text = $"[{match.Groups["linktext"].Value}]({match.Groups["linkurl"].Value})" });
                }
            }
            else if (match.Groups["imgalt"].Success)
            {
                inlines.Add(new Run { Text = $"[Image: {match.Groups["imgalt"].Value}]", FontStyle = global::Windows.UI.Text.FontStyle.Italic });
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex == 0)
        {
            inlines.Add(new Run { Text = text });
        }
        else if (lastIndex < text.Length)
        {
            inlines.Add(new Run { Text = text[lastIndex..] });
        }
    }

    internal static Brush GetThemeBrush(string resourceKey, string fallbackResourceKey)
    {
        try
        {
            return (Brush)Application.Current.Resources[resourceKey];
        }
        catch
        {
            try
            {
                return (Brush)Application.Current.Resources[fallbackResourceKey];
            }
            catch
            {
                return new SolidColorBrush(Colors.Gold);
            }
        }
    }

    [GeneratedRegex(@"\*\*\*(?<bolditalic>.+?)\*\*\*|\*\*(?<bold>.+?)\*\*|(?<!\*)\*(?!\*)(?<italic>.+?)(?<!\*)\*(?!\*)|`(?<code>.+?)`|!\[(?<imgalt>.+?)\]\((?<imgurl>.+?)\)|\[(?<linktext>.+?)\]\((?<linkurl>.+?)\)")]
    private static partial Regex InlineMarkdownPattern();
}
