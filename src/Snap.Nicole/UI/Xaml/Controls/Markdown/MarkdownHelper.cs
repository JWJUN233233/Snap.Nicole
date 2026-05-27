using Microsoft.Extensions.Primitives;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

internal static partial class MarkdownHelper
{
    private static readonly char[] LineFeedSeparators = ['\n'];
    private static readonly char[] CarriageReturnSeparators = ['\r'];
    private static readonly char[] TableCellSeparators = ['|'];
    private const int ListIndentColumnsPerLevel = 2;
    private const int TabIndentColumns = 4;
    private const double ListItemIndent = 16;
    private const double NestedListItemIndent = 20;
    private const string TaskListIconFontFamilyName = "Segoe Fluent Icons";
    private const string TaskListUncheckedGlyph = "\uE739";
    private const string TaskListCheckedGlyph = "\uE73A";

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
        int codeFenceLength = 0;
        StringTokenizer tokenizer = new(markdown, GetLineSeparators(markdown));
        StringTokenizer.Enumerator enumerator = tokenizer.GetEnumerator();
        bool hasPendingLine = false;
        StringSegment pendingLine = default;

        while (TryReadLine(ref enumerator, ref hasPendingLine, ref pendingLine, out StringSegment line))
        {
            StringSegment trimmedStart = line.TrimStart();

            if (!inCodeBlock && TryParseBacktickCodeFence(trimmedStart, 3, out int openingFenceLength, out StringSegment fenceInfo))
            {
                inCodeBlock = true;
                codeFenceLength = openingFenceLength;
                codeLanguage = fenceInfo.Trim().ToString();

                continue;
            }

            if (inCodeBlock)
            {
                if (TryParseBacktickCodeFence(trimmedStart, codeFenceLength, out _, out StringSegment closingFenceInfo)
                    && IsWhiteSpace(closingFenceInfo))
                {
                    AddCodeBlock(richText, GetCodeBlockText(codeBuffer), codeLanguage);
                    codeBuffer?.Clear();
                    codeLanguage = string.Empty;
                    codeFenceLength = 0;
                    inCodeBlock = false;
                    continue;
                }

                codeBuffer ??= new StringBuilder();
                codeBuffer.Append(line.AsSpan());
                codeBuffer.Append('\n');
                continue;
            }

            if (IsWhiteSpace(line))
            {
                continue;
            }

            if (TryParseHeading(line, out StringSegment headingText, out double headingFontSize))
            {
                AddHeading(richText, headingText, headingFontSize);
            }
            else if (IsHorizontalRule(line))
            {
                AddHorizontalRule(richText);
            }
            else if (IsTableLine(line) && TryReadLine(ref enumerator, ref hasPendingLine, ref pendingLine, out StringSegment separatorLine))
            {
                if (IsTableSeparator(separatorLine))
                {
                    AddTable(richText, line, separatorLine, ref enumerator, ref hasPendingLine, ref pendingLine);
                }
                else
                {
                    pendingLine = separatorLine;
                    hasPendingLine = true;
                    AddParagraph(richText, line);
                }
            }
            else if (TryParseBlockquote(line, out int blockquoteDepth, out StringSegment blockquoteText))
            {
                AddBlockquote(richText, blockquoteDepth, blockquoteText);
            }
            else if (TryParseTaskListItem(line, out bool isTaskChecked, out StringSegment taskText, out int taskListDepth))
            {
                AddTaskListItem(richText, isTaskChecked, taskText, taskListDepth);
            }
            else if (TryParseUnorderedListItem(line, out StringSegment listItemText, out int unorderedListDepth))
            {
                AddListItem(richText, listItemText, "- ", unorderedListDepth);
            }
            else if (TryParseOrderedListItem(line, out StringSegment listNumber, out StringSegment listText, out int orderedListDepth))
            {
                AddListItem(richText, listText, CreateOrderedListBullet(listNumber), orderedListDepth);
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
        if (line.Length > 0 && line[^1] == '\r')
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

    private static bool TryParseHeading(StringSegment line, out StringSegment text, out double fontSize)
    {
        StringSegment trimmed = line.TrimStart();
        int level = 0;

        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        if (level is < 1 or > 6 || level >= trimmed.Length || !char.IsWhiteSpace(trimmed[level]))
        {
            text = default;
            fontSize = 0;
            return false;
        }

        int textOffset = level;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        text = Slice(trimmed, textOffset);
        fontSize = level switch
        {
            1 => 28,
            2 => 21,
            3 => 17.5,
            4 => 14,
            5 => 12.25,
            6 => 11.9,
            _ => 14,
        };
        return true;
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

    private static bool TryParseBacktickCodeFence(StringSegment trimmedStart, int minimumFenceLength, out int fenceLength, out StringSegment fenceInfo)
    {
        fenceLength = 0;
        fenceInfo = default;

        while (fenceLength < trimmedStart.Length && trimmedStart[fenceLength] == '`')
        {
            fenceLength++;
        }

        if (fenceLength < minimumFenceLength)
        {
            return false;
        }

        fenceInfo = Slice(trimmedStart, fenceLength);
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

    private static bool TryParseBlockquote(StringSegment line, out int depth, out StringSegment text)
    {
        StringSegment trimmed = line.TrimStart();

        if (trimmed.Length == 0 || trimmed[0] != '>')
        {
            depth = 0;
            text = default;
            return false;
        }

        int textOffset = 0;
        depth = 0;
        while (textOffset < trimmed.Length && trimmed[textOffset] == '>')
        {
            textOffset++;
            depth++;

            while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
            {
                textOffset++;
            }
        }

        text = Slice(trimmed, textOffset);
        return true;
    }

    private static bool TryParseTaskListItem(StringSegment line, out bool isChecked, out StringSegment text, out int depth)
    {
        if (!TryParseUnorderedListItem(line, out StringSegment listText, out depth))
        {
            isChecked = false;
            text = default;
            depth = 0;
            return false;
        }

        StringSegment trimmed = listText.TrimStart();
        if (trimmed.Length < 3 || trimmed[0] != '[' || trimmed[2] != ']')
        {
            isChecked = false;
            text = default;
            depth = 0;
            return false;
        }

        char marker = trimmed[1];
        if (marker is not ' ' and not 'x' and not 'X')
        {
            isChecked = false;
            text = default;
            depth = 0;
            return false;
        }

        int textOffset = 3;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        isChecked = marker is 'x' or 'X';
        text = Slice(trimmed, textOffset);
        return true;
    }

    private static bool TryParseUnorderedListItem(StringSegment line, out StringSegment text, out int depth)
    {
        StringSegment trimmed = line.TrimStart();

        if (trimmed.Length < 2 || trimmed[0] is not '-' and not '*' and not '+' || !char.IsWhiteSpace(trimmed[1]))
        {
            text = default;
            depth = 0;
            return false;
        }

        int textOffset = 2;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        text = Slice(trimmed, textOffset);
        depth = GetListDepth(line);
        return true;
    }

    private static bool TryParseOrderedListItem(StringSegment line, out StringSegment number, out StringSegment text, out int depth)
    {
        StringSegment trimmed = line.TrimStart();
        ReadOnlySpan<char> span = trimmed.AsSpan();
        int index = 0;

        while (index < span.Length && char.IsDigit(span[index]))
        {
            index++;
        }

        if (index == 0 || index + 1 >= span.Length || span[index] != '.' || !char.IsWhiteSpace(span[index + 1]))
        {
            number = default;
            text = default;
            depth = 0;
            return false;
        }

        int textOffset = index + 2;
        while (textOffset < trimmed.Length && char.IsWhiteSpace(trimmed[textOffset]))
        {
            textOffset++;
        }

        number = Slice(trimmed, 0, index);
        text = Slice(trimmed, textOffset);
        depth = GetListDepth(line);
        return true;
    }

    private static int GetListDepth(StringSegment line)
    {
        int columns = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char current = line[i];
            if (current == ' ')
            {
                columns++;
            }
            else if (current == '\t')
            {
                columns += TabIndentColumns - columns % TabIndentColumns;
            }
            else if (char.IsWhiteSpace(current))
            {
                columns++;
            }
            else
            {
                break;
            }
        }

        return columns / ListIndentColumnsPerLevel;
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
        Paragraph paragraph = new() { Margin = new Thickness(0) };
        paragraph.Inlines.Add(new InlineUIContainer
        {
            Child = new Border
            {
                Margin = new Thickness(0, 4, 0, 4),
                Height = 1,
                MinWidth = 100000,
                Background = GetThemeBrush("ControlStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            },
        });
        richText.Blocks.Add(paragraph);
    }

    private static void AddBlockquote(RichTextBlock richText, int depth, StringSegment text)
    {
        RichTextBlock quoteText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            IsTextSelectionEnabled = true,
        };

        Paragraph quoteParagraph = new()
        {
            Margin = new Thickness(0),
            Foreground = GetThemeBrush("TextFillColorSecondaryBrush", "SystemControlForegroundBaseMediumBrush"),
        };
        AddInlineMarkdown(quoteParagraph.Inlines, text);
        quoteText.Blocks.Add(quoteParagraph);

        Brush borderBrush = GetThemeBrush("TextFillColorSecondaryBrush", "SystemControlForegroundBaseMediumBrush");
        UIElement quoteElement = quoteText;
        for (int i = 0; i < depth; i++)
        {
            quoteElement = new Border
            {
                Padding = new Thickness(8, i == 0 ? 2 : 0, 0, i == 0 ? 2 : 0),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(2, 0, 0, 0),
                Child = quoteElement,
            };
        }

        Paragraph container = new() { Margin = new Thickness(0, 2, 0, 2) };
        container.Inlines.Add(new InlineUIContainer
        {
            Child = quoteElement,
        });
        richText.Blocks.Add(container);
    }

    private static void AddTaskListItem(RichTextBlock richText, bool isChecked, StringSegment text, int depth)
    {
        Paragraph paragraph = new() { Margin = CreateListItemMargin(depth) };
        paragraph.Inlines.Add(new InlineUIContainer
        {
            Child = new FontIcon
            {
                FontFamily = new FontFamily(TaskListIconFontFamilyName),
                FontSize = 14,
                Glyph = isChecked ? TaskListCheckedGlyph : TaskListUncheckedGlyph,
                Height = 16,
                IsHitTestVisible = false,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 16,
            },
        });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddListItem(RichTextBlock richText, StringSegment text, string bullet, int depth)
    {
        Paragraph paragraph = new() { Margin = CreateListItemMargin(depth) };
        paragraph.Inlines.Add(new Run { Text = bullet });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static Thickness CreateListItemMargin(int depth)
    {
        return new Thickness(ListItemIndent + depth * NestedListItemIndent, 1, 0, 1);
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
            && trimmed[trimmed.Length - 1] == '|'
            && !IsEscaped(trimmed, trimmed.Length - 1);
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
        int cellStart = 0;

        for (int i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] != '|' || IsEscaped(trimmed, i))
            {
                continue;
            }

            cells.Add(Slice(trimmed, cellStart, i - cellStart).Trim().ToString());
            cellStart = i + 1;
        }

        cells.Add(Slice(trimmed, cellStart, trimmed.Length - cellStart).Trim().ToString());
        return cells;
    }

    private static List<TextAlignment> ParseTableColumnAlignments(StringSegment line, int columnCount)
    {
        StringSegment trimmed = TrimTableCellBounds(line);
        List<TextAlignment> alignments = [];
        StringTokenizer tokenizer = new(trimmed, TableCellSeparators);

        foreach (StringSegment cell in tokenizer)
        {
            alignments.Add(ParseTableColumnAlignment(cell));
        }

        while (alignments.Count < columnCount)
        {
            alignments.Add(TextAlignment.Left);
        }

        return alignments;
    }

    private static TextAlignment ParseTableColumnAlignment(StringSegment cell)
    {
        StringSegment trimmed = cell.Trim();
        bool hasLeftMarker = trimmed.Length > 0 && trimmed[0] == ':';
        bool hasRightMarker = trimmed.Length > 0 && trimmed[trimmed.Length - 1] == ':';

        if (hasLeftMarker && hasRightMarker)
        {
            return TextAlignment.Center;
        }

        if (hasRightMarker)
        {
            return TextAlignment.Right;
        }

        return TextAlignment.Left;
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
        StringSegment separatorLine,
        ref StringTokenizer.Enumerator enumerator,
        ref bool hasPendingLine,
        ref StringSegment pendingLine)
    {
        List<string> headerCells = ParseTableCells(headerLine);
        int columnCount = headerCells.Count;
        List<global::Microsoft.UI.Xaml.TextAlignment> columnAlignments = ParseTableColumnAlignments(separatorLine, columnCount);
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
                ColumnAlignments = columnAlignments,
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
        AddInlineMarkdown(inlines, text, null);
    }

    private static void AddInlineMarkdown(InlineCollection inlines, string text, global::Windows.UI.Text.TextDecorations? textDecorations)
    {
        if (text.Length == 0)
        {
            return;
        }

        Regex pattern = InlineMarkdownPattern();
        int lastIndex = 0;
        int searchIndex = 0;

        while (searchIndex < text.Length)
        {
            Match match = pattern.Match(text, searchIndex);
            if (!match.Success)
            {
                break;
            }

            if (IsInlineMarkdownMatchEscaped(text, match))
            {
                searchIndex = match.Index + 1;
                continue;
            }

            if (match.Index > lastIndex)
            {
                AddTextRun(inlines, text[lastIndex..match.Index], textDecorations);
            }

            if (match.Groups["bolditalic"].Success)
            {
                Bold bold = new();
                Italic italic = new();
                AddInlineMarkdown(italic.Inlines, match.Groups["bolditalic"].Value, textDecorations);
                bold.Inlines.Add(italic);
                inlines.Add(bold);
            }
            else if (match.Groups["bold"].Success)
            {
                Bold bold = new();
                AddInlineMarkdown(bold.Inlines, match.Groups["bold"].Value, textDecorations);
                inlines.Add(bold);
            }
            else if (match.Groups["italic"].Success)
            {
                Italic italic = new();
                AddInlineMarkdown(italic.Inlines, match.Groups["italic"].Value, textDecorations);
                inlines.Add(italic);
            }
            else if (match.Groups["strikethrough"].Success)
            {
                AddInlineMarkdown(
                    inlines,
                    match.Groups["strikethrough"].Value,
                    CombineTextDecorations(textDecorations, global::Windows.UI.Text.TextDecorations.Strikethrough));
            }
            else if (match.Groups["highlight"].Success)
            {
                AddHighlight(inlines, UnescapeMarkdownText(match.Groups["highlight"].Value));
            }
            else if (match.Groups["code"].Success)
            {
                inlines.Add(new InlineUIContainer
                {
                    Child = new MarkdownInlineCodeView
                    {
                        Text = NormalizeInlineCodeText(match.Groups["code"].Value),
                    },
                });
            }
            else if (match.Groups["linktext"].Success)
            {
                AddLink(
                    inlines,
                    UnescapeMarkdownText(match.Groups["linktext"].Value),
                    match.Groups["linkurl"].Value,
                    match.Groups["linktitle"].Value);
            }
            else if (match.Groups["imgalt"].Success)
            {
                AddImage(
                    inlines,
                    UnescapeMarkdownText(match.Groups["imgalt"].Value),
                    match.Groups["imgurl"].Value,
                    match.Groups["imgtitle"].Value);
            }

            lastIndex = match.Index + match.Length;
            searchIndex = lastIndex;
        }

        if (lastIndex == 0)
        {
            AddTextRun(inlines, text, textDecorations);
        }
        else if (lastIndex < text.Length)
        {
            AddTextRun(inlines, text[lastIndex..], textDecorations);
        }
    }

    private static global::Windows.UI.Text.TextDecorations CombineTextDecorations(
        global::Windows.UI.Text.TextDecorations? current,
        global::Windows.UI.Text.TextDecorations additional)
    {
        if (current.HasValue)
        {
            return current.Value | additional;
        }

        return additional;
    }

    private static string NormalizeInlineCodeText(string code)
    {
        string normalized = code.ReplaceLineEndings(" ");
        if (normalized.Length < 2 || normalized[0] != ' ' || normalized[^1] != ' ')
        {
            return normalized;
        }

        for (int i = 1; i < normalized.Length - 1; i++)
        {
            if (normalized[i] != ' ')
            {
                return normalized[1..^1];
            }
        }

        return normalized;
    }

    private static void AddTextRun(InlineCollection inlines, string text, global::Windows.UI.Text.TextDecorations? textDecorations)
    {
        string unescapedText = UnescapeMarkdownText(text);
        if (unescapedText.Length == 0)
        {
            return;
        }

        Run run = new()
        {
            Text = unescapedText,
        };

        if (textDecorations.HasValue)
        {
            run.TextDecorations = textDecorations.Value;
        }

        inlines.Add(run);
    }

    private static bool IsInlineMarkdownMatchEscaped(string text, Match match)
    {
        if (IsEscaped(text, match.Index))
        {
            return true;
        }

        if (match.Groups["bolditalic"].Success)
        {
            return IsEscaped(text, match.Index + match.Length - 3);
        }

        if (match.Groups["bold"].Success
            || match.Groups["strikethrough"].Success
            || match.Groups["highlight"].Success)
        {
            return IsEscaped(text, match.Index + match.Length - 2);
        }

        if (match.Groups["italic"].Success)
        {
            return IsEscaped(text, match.Index + match.Length - 1);
        }

        return false;
    }

    private static bool IsEscaped(string text, int index)
    {
        int backslashCount = 0;
        for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
        {
            backslashCount++;
        }

        return backslashCount % 2 != 0;
    }

    private static bool IsEscaped(StringSegment text, int index)
    {
        int backslashCount = 0;
        for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
        {
            backslashCount++;
        }

        return backslashCount % 2 != 0;
    }

    private static string UnescapeMarkdownText(string text)
    {
        if (!text.Contains('\\'))
        {
            return text;
        }

        StringBuilder? builder = null;
        int textStart = 0;

        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] != '\\' || !IsMarkdownEscapableCharacter(text[i + 1]))
            {
                continue;
            }

            builder ??= new StringBuilder(text.Length);
            builder.Append(text.AsSpan(textStart, i - textStart));
            builder.Append(text[i + 1]);
            i++;
            textStart = i + 1;
        }

        if (builder is null)
        {
            return text;
        }

        builder.Append(text.AsSpan(textStart));
        return builder.ToString();
    }

    private static bool IsMarkdownEscapableCharacter(char character)
    {
        return character is >= '!' and <= '~'
            && (char.IsPunctuation(character) || char.IsSymbol(character));
    }

    private static void AddHighlight(InlineCollection inlines, string text)
    {
        inlines.Add(new InlineUIContainer
        {
            Child = new Border
            {
                Padding = new Thickness(2, 0, 2, 0),
                CornerRadius = new CornerRadius(2),
                Background = GetThemeBrush("SystemFillColorCautionBackgroundBrush", "SystemControlHighlightAccentBrush"),
                Child = new TextBlock
                {
                    Foreground = GetThemeBrush("TextFillColorPrimaryBrush", "SystemControlForegroundBaseHighBrush"),
                    Text = text,
                    TextWrapping = TextWrapping.NoWrap,
                },
            },
        });
    }

    private static void AddLink(InlineCollection inlines, string text, string url, string title)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            inlines.Add(new Run { Text = CreateLinkFallback(text, url, title) });
            return;
        }

        Hyperlink link = new()
        {
            NavigateUri = uri,
        };
        link.Inlines.Add(new Run { Text = text });

        if (!string.IsNullOrEmpty(title))
        {
            ToolTipService.SetToolTip(link, title);
        }

        inlines.Add(link);
    }

    private static void AddImage(InlineCollection inlines, string altText, string url, string title)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            AddImageFallback(inlines, altText, url, title);
            return;
        }

        Image image = new()
        {
            MaxHeight = 240,
            MaxWidth = 360,
            Source = new BitmapImage
            {
                UriSource = uri,
            },
            Stretch = Stretch.Uniform,
        };

        string tooltip = string.IsNullOrEmpty(title) ? altText : title;
        if (!string.IsNullOrEmpty(tooltip))
        {
            ToolTipService.SetToolTip(image, tooltip);
        }

        inlines.Add(new InlineUIContainer
        {
            Child = image,
        });
    }

    private static void AddImageFallback(InlineCollection inlines, string altText, string url, string title)
    {
        inlines.Add(new Run
        {
            Text = CreateImageFallback(altText, url, title),
            FontStyle = global::Windows.UI.Text.FontStyle.Italic,
        });
    }

    private static string CreateLinkFallback(string text, string url, string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return $"[{text}]({url})";
        }

        return $"[{text}]({url} \"{title}\")";
    }

    private static string CreateImageFallback(string altText, string url, string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return $"![{altText}]({url})";
        }

        return $"![{altText}]({url} \"{title}\")";
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

    [GeneratedRegex(@"\*\*\*(?<bolditalic>(?:\\.|.)+?)\*\*\*|\*\*(?<bold>(?:\\.|.)+?)\*\*|(?<!\*)\*(?!\*)(?<italic>(?:\\.|.)+?)(?<!\*)\*(?!\*)|~~(?<strikethrough>(?:\\.|.)+?)~~|==(?<highlight>(?:\\.|.)+?)==|(?<codeDelimiter>`+)(?<code>.*?)(?<!`)\k<codeDelimiter>(?!`)|!\[(?<imgalt>(?:\\.|[^\]])*?)\]\((?<imgurl>\S+?)(?:\s+""(?<imgtitle>[^""]*?)"")?\)|\[(?<linktext>(?:\\.|[^\]])+?)\]\((?<linkurl>\S+?)(?:\s+""(?<linktitle>[^""]*?)"")?\)")]
    private static partial Regex InlineMarkdownPattern();
}
