using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Snap.Nicole.Services.AI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Snap.Nicole.UI.Xaml.Helpers;

internal static partial class MarkdownHelper
{
    public static FrameworkElement CreateMessageBubble(ChatRole role, string content, string? modelId = null)
    {
        StackPanel panel = new() { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Stretch };

        string headerText = role == ChatRole.User ? "You" : (modelId ?? "AI");
        TextBlock header = new()
        {
            Text = headerText,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Opacity = 0.6,
        };
        panel.Children.Add(header);

        if (role == ChatRole.User)
        {
            TextBlock textBlock = new()
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                IsTextSelectionEnabled = true,
            };
            panel.Children.Add(textBlock);
        }
        else
        {
            RichTextBlock richText = ParseMarkdown(content);
            panel.Children.Add(richText);
        }

        Border border = new()
        {
            Child = panel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0, 0, 0, 8),
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
        };

        return border;
    }

    private static RichTextBlock ParseMarkdown(string markdown)
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

            // Headers
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
            // Horizontal rule
            else if (Regex.IsMatch(line.Trim(), @"^[-*_]{3,}$"))
            {
                Paragraph hrParagraph = new() { Margin = new Thickness(0, 4, 0, 4) };
                richText.Blocks.Add(hrParagraph);
            }
            // Table: detect lines with leading and trailing '|'
            else if (IsTableLine(line) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
            {
                List<string> tableLines = [line, lines[i + 1]];
                i += 2;
                while (i < lines.Length && IsTableLine(lines[i]))
                {
                    tableLines.Add(lines[i]);
                    i++;
                }
                i--; // will be incremented by the for loop
                AddTable(richText, tableLines);
            }
            // Blockquote
            else if (line.StartsWith("> "))
            {
                AddBlockquote(richText, line[2..]);
            }
            // Unordered list
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                AddListItem(richText, line[2..], "• ");
            }
            // Ordered list
            else if (Regex.IsMatch(line, @"^\d+\.\s"))
            {
                string num = Regex.Match(line, @"^(\d+)\.").Groups[1].Value;
                string text = Regex.Replace(line, @"^\d+\.\s", "");
                AddListItem(richText, text, $"{num}. ");
            }
            // Paragraph
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

    private static void AddBlockquote(RichTextBlock richText, string text)
    {
        Paragraph paragraph = new()
        {
            Margin = new Thickness(12, 2, 0, 2),
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        };
        paragraph.Inlines.Add(new Run { Text = "│  " });
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
        ScrollViewer scrollViewer = new()
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Auto,
            MaxHeight = 400,
            Margin = new Thickness(0, 4, 0, 4),
        };

        TextBlock codeText = new()
        {
            Text = code,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
            FontSize = 13,
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.NoWrap,
        };

        Border codeBorder = new()
        {
            Child = scrollViewer,
            Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", "SystemControlBackgroundChromeMediumLowBrush"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            BorderThickness = new Thickness(1),
        };

        scrollViewer.Content = codeText;

        Paragraph container = new();
        container.Inlines.Add(new InlineUIContainer { Child = codeBorder });
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
        // Remove leading/trailing '|', then split by '|'
        string trimmed = line.Trim().Trim('|');
        return [.. trimmed.Split('|').Select(c => c.Trim())];
    }

    private static void AddTable(RichTextBlock richText, List<string> tableLines)
    {
        if (tableLines.Count < 2) return;

        // First line = header, second line = separator, rest = body
        List<string> headerCells = ParseTableCells(tableLines[0]);
        int columnCount = headerCells.Count;

        Grid tableGrid = new()
        {
            Margin = new Thickness(0, 6, 0, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", "SystemControlBackgroundChromeMediumLowBrush"),
        };

        for (int c = 0; c < columnCount; c++)
        {
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        }

        // Header row
        AddTableRow(tableGrid, headerCells, 0, isHeader: true);

        // Body rows (skip separator at index 1)
        for (int r = 2; r < tableLines.Count; r++)
        {
            List<string> cells = ParseTableCells(tableLines[r]);
            // Pad if fewer cells than columns
            while (cells.Count < columnCount) cells.Add("");
            AddTableRow(tableGrid, cells, r - 1, isHeader: false);
        }

        Paragraph container = new();
        container.Inlines.Add(new InlineUIContainer { Child = tableGrid });
        richText.Blocks.Add(container);
    }

    private static void AddTableRow(Grid grid, List<string> cells, int rowIndex, bool isHeader)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int c = 0; c < cells.Count; c++)
        {
            RichTextBlock cellRich = new()
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                IsTextSelectionEnabled = true,
            };
            Paragraph richParagraph = new() { Margin = new Thickness(0) };
            AddInlineMarkdown(richParagraph.Inlines, cells[c]);
            if (isHeader) richParagraph.FontWeight = FontWeights.SemiBold;
            cellRich.Blocks.Add(richParagraph);

            Border cellBorder = new()
            {
                Child = cellRich,
                Padding = new Thickness(8, 4, 8, 4),
                BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", "SystemControlForegroundBaseLowBrush"),
                BorderThickness = c < cells.Count - 1
                    ? new Thickness(0, 0, 1, 0)
                    : new Thickness(0),
            };

            Grid.SetRow(cellBorder, rowIndex);
            Grid.SetColumn(cellBorder, c);
            grid.Children.Add(cellBorder);
        }

        if (isHeader)
        {
            foreach (UIElement child in grid.Children)
            {
                if (child is Border b && Grid.GetRow(b) == 0)
                {
                    b.BorderThickness = new Thickness(
                        b.BorderThickness.Left,
                        b.BorderThickness.Top,
                        b.BorderThickness.Right,
                        1);
                }
            }
        }
    }

    private static void AddInlineMarkdown(InlineCollection inlines, string text)
    {
        // Pattern: ***bold+italic***, **bold**, *italic*, `code`, ![alt](url), [text](url)
        Regex pattern = InlineMarkdownPattern();
        int lastIndex = 0;

        foreach (Match match in pattern.Matches(text))
        {
            // Add preceding plain text
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
                Run codeRun = new()
                {
                    Text = match.Groups["code"].Value,
                    FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                    FontSize = 12.5,
                };
                InlineUIContainer container = new();
                Border border = new()
                {
                    Child = new TextBlock
                    {
                        Inlines = { codeRun },
                        FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                        FontSize = 12.5,
                    },
                    Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", "SystemControlBackgroundChromeMediumLowBrush"),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                };
                container.Child = border;
                inlines.Add(container);
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
            // Images: show as [Image: alt] link
            else if (match.Groups["imgalt"].Success)
            {
                inlines.Add(new Run { Text = $"[Image: {match.Groups["imgalt"].Value}]", FontStyle = global::Windows.UI.Text.FontStyle.Italic });
            }

            lastIndex = match.Index + match.Length;
        }

        // Remaining text
        if (lastIndex < text.Length)
        {
            inlines.Add(new Run { Text = text[lastIndex..] });
        }
    }

    private static Brush GetThemeBrush(string lightKey, string fallback)
    {
        try
        {
            return (Brush)Application.Current.Resources[lightKey];
        }
        catch
        {
            return new SolidColorBrush(Colors.LightGray);
        }
    }

    [GeneratedRegex(@"\*\*\*(?<bolditalic>.+?)\*\*\*|\*\*(?<bold>.+?)\*\*|(?<!\*)\*(?!\*)(?<italic>.+?)(?<!\*)\*(?!\*)|`(?<code>.+?)`|!\[(?<imgalt>.+?)\]\((?<imgurl>.+?)\)|\[(?<linktext>.+?)\]\((?<linkurl>.+?)\)")]
    private static partial Regex InlineMarkdownPattern();
}
