using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Snap.Nicole.UI.Xaml.Controls.ChatElements;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Snap.Nicole.UI.Xaml.Helpers;

internal static partial class MarkdownHelper
{
    public static RichTextBlock CreateMarkdownBlock(string? markdown)
    {
        RichTextBlock richText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            IsTextSelectionEnabled = true,
        };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return richText;
        }

        string[] lines = markdown.Split('\n');
        bool inCodeBlock = false;
        string codeBuffer = "";
        string codeLanguage = "";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.TrimStart().StartsWith("```"))
            {
                if (inCodeBlock)
                {
                    AddCodeBlock(richText, codeBuffer.TrimEnd('\r', '\n'), codeLanguage);
                    codeBuffer = "";
                    codeLanguage = "";
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                    codeLanguage = line.TrimStart()[3..].Trim();
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeBuffer += line + "\n";
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("### "))
            {
                AddHeading(richText, line[4..], 18);
            }
            else if (line.StartsWith("## "))
            {
                AddHeading(richText, line[3..], 20);
            }
            else if (line.StartsWith("# "))
            {
                AddHeading(richText, line[2..], 24);
            }
            else if (Regex.IsMatch(line.Trim(), @"^[-*_]{3,}$"))
            {
                AddHorizontalRule(richText);
            }
            else if (IsTableLine(line) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
            {
                List<string> tableLines = [line, lines[i + 1]];
                i += 2;

                while (i < lines.Length && IsTableLine(lines[i]))
                {
                    tableLines.Add(lines[i]);
                    i++;
                }

                i--;
                AddTable(richText, tableLines);
            }
            else if (line.StartsWith("> "))
            {
                AddBlockquote(richText, line[2..]);
            }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                AddListItem(richText, line[2..], "- ");
            }
            else if (Regex.IsMatch(line, @"^\d+\.\s"))
            {
                string num = Regex.Match(line, @"^(\d+)\.").Groups[1].Value;
                string text = Regex.Replace(line, @"^\d+\.\s", "");
                AddListItem(richText, text, $"{num}. ");
            }
            else
            {
                AddParagraph(richText, line);
            }
        }

        if (inCodeBlock)
        {
            AddCodeBlock(richText, codeBuffer.TrimEnd('\r', '\n'), codeLanguage);
        }

        return richText;
    }

    private static void AddHeading(RichTextBlock richText, string text, double fontSize)
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

    private static void AddBlockquote(RichTextBlock richText, string text)
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

    private static void AddListItem(RichTextBlock richText, string text, string bullet)
    {
        Paragraph paragraph = new() { Margin = new Thickness(16, 1, 0, 1) };
        paragraph.Inlines.Add(new Run { Text = bullet });
        AddInlineMarkdown(paragraph.Inlines, text);
        richText.Blocks.Add(paragraph);
    }

    private static void AddParagraph(RichTextBlock richText, string text)
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

    private static bool IsTableLine(string line)
    {
        string trimmed = line.Trim();
        return trimmed.StartsWith('|') && trimmed.EndsWith('|') && trimmed.Length > 2;
    }

    private static bool IsTableSeparator(string line)
    {
        return Regex.IsMatch(line.Trim(), @"^\|[\s\-:|]+\|$");
    }

    private static List<string> ParseTableCells(string line)
    {
        string trimmed = line.Trim().Trim('|');
        return [.. trimmed.Split('|').Select(c => c.Trim())];
    }

    private static void AddTable(RichTextBlock richText, List<string> tableLines)
    {
        if (tableLines.Count < 2)
        {
            return;
        }

        List<string> headerCells = ParseTableCells(tableLines[0]);
        int columnCount = headerCells.Count;
        List<IReadOnlyList<string>> rows = [headerCells];

        for (int r = 2; r < tableLines.Count; r++)
        {
            List<string> cells = ParseTableCells(tableLines[r]);
            while (cells.Count < columnCount)
            {
                cells.Add("");
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

    internal static void AddInlineMarkdown(InlineCollection inlines, string text)
    {
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

        if (lastIndex < text.Length)
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
                return new SolidColorBrush(Colors.LightGray);
            }
        }
    }

    [GeneratedRegex(@"\*\*\*(?<bolditalic>.+?)\*\*\*|\*\*(?<bold>.+?)\*\*|(?<!\*)\*(?!\*)(?<italic>.+?)(?<!\*)\*(?!\*)|`(?<code>.+?)`|!\[(?<imgalt>.+?)\]\((?<imgurl>.+?)\)|\[(?<linktext>.+?)\]\((?<linkurl>.+?)\)")]
    private static partial Regex InlineMarkdownPattern();
}
