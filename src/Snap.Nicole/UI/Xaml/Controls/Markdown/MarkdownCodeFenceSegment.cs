namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

internal readonly ref struct MarkdownCodeFenceSegment(int fenceLength, ReadOnlySpan<char> fenceInfo)
{
    public int FenceLength { get; } = fenceLength;

    public ReadOnlySpan<char> FenceInfo { get; } = fenceInfo;
}
