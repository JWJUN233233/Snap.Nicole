using Microsoft.Extensions.Primitives;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Nicole.Core.Primitives;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

internal static partial class MarkdownHelper
{
    private static readonly char[] LineFeedSeparators = ['\n'];
    private static readonly char[] CarriageReturnSeparators = ['\r'];
    private static readonly char[] TableCellSeparators = ['|'];
    private const double ListItemIndent = 16;
    private const double NestedListItemIndent = 20;
    private const string TaskListIconFontFamilyName = "Segoe Fluent Icons";
    private const string TaskListUncheckedGlyph = "\uE739";
    private const string TaskListCheckedGlyph = "\uE73A";

    public static IReadOnlyList<UIElement> CreateMarkdownBlocks(string? markdown)
    {
        List<UIElement> blocks = [];
        RichTextBlock? richText = null;

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return blocks;
        }

        bool inCodeBlock = false;
        StringBuilder? codeBuffer = null;
        string codeLanguage = string.Empty;
        int codeFenceLength = 0;
        StringTokenizer tokenizer = new(markdown, GetLineSeparators(markdown));
        StringTokenizer.Enumerator enumerator = tokenizer.GetEnumerator();
        StringSegment? pendingLine = null;

        while (TryReadLine(ref enumerator, ref pendingLine, out StringSegment line))
        {
            StringSegment trimmedStart = line.TrimStart();

            if (!inCodeBlock
                && MarkdownSyntax.TryParseBacktickCodeFence(
                    trimmedStart,
                    MarkdownSyntax.MinimumCodeFenceLength,
                    out MarkdownSyntax.MarkdownCodeFenceSegment openingFence))
            {
                inCodeBlock = true;
                codeFenceLength = openingFence.FenceLength;
                codeLanguage = openingFence.FenceInfo.Trim().ToString();

                continue;
            }

            if (inCodeBlock)
            {
                if (MarkdownSyntax.TryParseBacktickCodeFence(
                        trimmedStart,
                        codeFenceLength,
                        out MarkdownSyntax.MarkdownCodeFenceSegment closingFence)
                    && closingFence.FenceInfo.IsWhiteSpace())
                {
                    AddCodeBlock(blocks, GetCodeBlockText(codeBuffer), codeLanguage);
                    codeBuffer?.Clear();
                    codeLanguage = string.Empty;
                    codeFenceLength = 0;
                    inCodeBlock = false;
                    richText = null;
                    continue;
                }

                codeBuffer ??= new StringBuilder();
                codeBuffer.Append(line.AsSpan());
                codeBuffer.Append('\n');
                continue;
            }

            if (line.IsWhiteSpace())
            {
                continue;
            }

            if (MarkdownSyntax.IsTableLine(line) && TryReadLine(ref enumerator, ref pendingLine, out StringSegment separatorLine))
            {
                if (MarkdownSyntax.IsTableSeparator(separatorLine))
                {
                    AddTable(blocks, line, separatorLine, ref enumerator, ref pendingLine);
                    richText = null;
                    continue;
                }

                pendingLine = separatorLine;
            }

            richText = EnsureRichTextBlock(blocks, richText);

            if (MarkdownSyntax.TryParseHeading(line, out StringSegment headingText, out int headingLevel))
            {
                AddHeading(richText, headingText, headingLevel);
            }
            else if (MarkdownSyntax.IsHorizontalRule(line))
            {
                AddHorizontalRule(richText);
            }
            else if (MarkdownSyntax.TryParseBlockquote(line, out int blockquoteDepth, out StringSegment blockquoteText))
            {
                AddBlockquote(richText, blockquoteDepth, blockquoteText);
            }
            else if (MarkdownSyntax.TryParseTaskListItem(line, out bool isTaskChecked, out StringSegment taskText, out int taskListDepth))
            {
                AddTaskListItem(richText, isTaskChecked, taskText, taskListDepth);
            }
            else if (MarkdownSyntax.TryParseUnorderedListItem(line, out StringSegment listItemText, out int unorderedListDepth))
            {
                AddListItem(richText, listItemText, "- ", unorderedListDepth);
            }
            else if (MarkdownSyntax.TryParseOrderedListItem(line, out StringSegment listNumber, out StringSegment listText, out int orderedListDepth))
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
            AddCodeBlock(blocks, GetCodeBlockText(codeBuffer), codeLanguage);
        }

        return blocks;
    }

    private static RichTextBlock EnsureRichTextBlock(List<UIElement> blocks, RichTextBlock? richText)
    {
        if (richText is not null)
        {
            return richText;
        }

        richText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14, // BodyTextBlockFontSize
            IsTextSelectionEnabled = true,
        };
        blocks.Add(richText);

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
        ref StringSegment? pendingLine,
        out StringSegment line)
    {
        if (pendingLine.HasValue)
        {
            line = pendingLine.GetValueOrDefault();
            pendingLine = null;
            return true;
        }

        if (!enumerator.MoveNext())
        {
            line = default;
            return false;
        }

        line = MarkdownSyntax.TrimTrailingCarriageReturn(enumerator.Current);
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

    private static string CreateOrderedListBullet(StringSegment number)
    {
        return string.Create(number.Length + 2, number, static (destination, segment) =>
        {
            segment.AsSpan().CopyTo(destination);
            destination[segment.Length] = '.';
            destination[segment.Length + 1] = ' ';
        });
    }

    private static void AddHeading(RichTextBlock richText, StringSegment text, int level)
    {
        Paragraph paragraph = new() { FontSize = GetHeadingFontSize(level), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 2) };
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static double GetHeadingFontSize(int level)
    {
        return level switch
        {
            1 => 28,
            2 => 21,
            3 => 17.5,
            4 => 14,
            5 => 12.25,
            6 => 11.9,
            _ => 14,
        };
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

    private static void AddCodeBlock(List<UIElement> blocks, string code, string language)
    {
        blocks.Add(new MarkdownCodeBlockView
        {
            Code = code,
            Language = language,
        });
    }

    private static List<string> ParseTableCells(StringSegment line)
    {
        StringSegment trimmed = MarkdownSyntax.TrimTableCellBounds(line);
        List<string> cells = [];
        int cellStart = 0;

        for (int i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] != '|' || MarkdownSyntax.IsEscaped(trimmed, i))
            {
                continue;
            }

            cells.Add(trimmed
                .Subsegment(cellStart, Math.Max(i - cellStart, 0))
                .Trim()
                .ToString());
            cellStart = i + 1;
        }

        cells.Add(trimmed
            .Subsegment(cellStart, Math.Max(trimmed.Length - cellStart, 0))
            .Trim()
            .ToString());
        return cells;
    }

    private static List<TextAlignment> ParseTableColumnAlignments(StringSegment line, int columnCount)
    {
        StringSegment trimmed = MarkdownSyntax.TrimTableCellBounds(line);
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

    private static void AddTable(
        List<UIElement> blocks,
        StringSegment headerLine,
        StringSegment separatorLine,
        ref StringTokenizer.Enumerator enumerator,
        ref StringSegment? pendingLine)
    {
        List<string> headerCells = ParseTableCells(headerLine);
        int columnCount = headerCells.Count;
        List<global::Microsoft.UI.Xaml.TextAlignment> columnAlignments = ParseTableColumnAlignments(separatorLine, columnCount);
        List<IReadOnlyList<string>> rows = [headerCells];

        while (TryReadLine(ref enumerator, ref pendingLine, out StringSegment line))
        {
            if (!MarkdownSyntax.IsTableLine(line))
            {
                pendingLine = line;
                break;
            }

            List<string> cells = ParseTableCells(line);
            while (cells.Count < columnCount)
            {
                cells.Add(string.Empty);
            }

            rows.Add(cells);
        }

        blocks.Add(new MarkdownTableView
        {
            ColumnAlignments = columnAlignments,
            Rows = rows,
        });
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
                AddHighlight(inlines, MarkdownSyntax.UnescapeMarkdownText(match.Groups["highlight"].Value));
            }
            else if (match.Groups["code"].Success)
            {
                inlines.Add(new InlineUIContainer
                {
                    Child = new MarkdownInlineCodeView
                    {
                        Text = MarkdownSyntax.NormalizeInlineCodeText(match.Groups["code"].Value),
                    },
                });
            }
            else if (match.Groups["linktext"].Success)
            {
                AddLink(
                    inlines,
                    MarkdownSyntax.UnescapeMarkdownText(match.Groups["linktext"].Value),
                    match.Groups["linkurl"].Value,
                    match.Groups["linktitle"].Value);
            }
            else if (match.Groups["imgalt"].Success)
            {
                AddImage(
                    inlines,
                    MarkdownSyntax.UnescapeMarkdownText(match.Groups["imgalt"].Value),
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

    private static void AddTextRun(InlineCollection inlines, string text, global::Windows.UI.Text.TextDecorations? textDecorations)
    {
        string unescapedText = MarkdownSyntax.UnescapeMarkdownText(text);
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
        if (MarkdownSyntax.IsEscaped(text, match.Index))
        {
            return true;
        }

        if (match.Groups["bolditalic"].Success)
        {
            return MarkdownSyntax.IsEscaped(text, match.Index + match.Length - 3);
        }

        if (match.Groups["bold"].Success
            || match.Groups["strikethrough"].Success
            || match.Groups["highlight"].Success)
        {
            return MarkdownSyntax.IsEscaped(text, match.Index + match.Length - 2);
        }

        if (match.Groups["italic"].Success)
        {
            return MarkdownSyntax.IsEscaped(text, match.Index + match.Length - 1);
        }

        return false;
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
