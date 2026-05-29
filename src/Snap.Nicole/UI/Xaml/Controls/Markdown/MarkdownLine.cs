namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

internal readonly struct MarkdownLine(int start, int contentEnd, int end)
{
    public int Start { get; } = start;

    public int ContentEnd { get; } = contentEnd;

    public int End { get; } = end;

    public ReadOnlySpan<char> GetContent(ReadOnlySpan<char> markdown)
    {
        return markdown[Start..ContentEnd];
    }
}
