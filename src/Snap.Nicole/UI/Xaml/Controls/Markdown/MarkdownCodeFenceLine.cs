namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

internal readonly struct MarkdownCodeFenceLine(int fenceLength, int fenceInfoStart)
{
    public int FenceLength { get; } = fenceLength;

    public int FenceInfoStart { get; } = fenceInfoStart;
}
