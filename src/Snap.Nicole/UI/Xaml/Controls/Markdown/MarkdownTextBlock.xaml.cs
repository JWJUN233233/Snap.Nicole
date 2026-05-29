using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Diagnostics;

namespace Snap.Nicole.UI.Xaml.Controls.Markdown;

[GeneratedDependencyProperty<string>("Markdown", PropertyChangedCallbackName = nameof(OnMarkdownChanged))]
internal sealed partial class MarkdownTextBlock : UserControl
{
    private const int PreferredStableChunkLength = 4096;

    private readonly List<MarkdownRenderChunk> stableChunks = [];
    private string renderedMarkdown = string.Empty;
    private int stableLength;
    private UIElement? tailElement;

    public MarkdownTextBlock()
    {
        InitializeComponent();
        RenderMarkdown(Markdown);
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock view)
        {
            view.RenderMarkdown(e.NewValue as string);
        }
    }

    private void RenderMarkdown(string? markdown)
    {
        Debug.WriteLine($"RenderMarkdown: '{markdown}'");
        string nextMarkdown = markdown ?? string.Empty;

        if (!IsAppendUpdate(nextMarkdown))
        {
            ResetRenderingState();
        }

        RenderAppendUpdate(nextMarkdown);
        renderedMarkdown = nextMarkdown;
    }

    private bool IsAppendUpdate(string markdown)
    {
        return markdown.Length >= renderedMarkdown.Length
            && markdown.StartsWith(renderedMarkdown, StringComparison.Ordinal);
    }

    private void ResetRenderingState()
    {
        stableChunks.Clear();
        ContentHost.Children.Clear();
        renderedMarkdown = string.Empty;
        stableLength = 0;
        tailElement = null;
    }

    private void RenderAppendUpdate(string markdown)
    {
        int nextStableLength = MarkdownBlockPartitioner.GetStablePrefixLength(markdown, stableLength);
        if (nextStableLength < stableLength)
        {
            ResetRenderingState();
            nextStableLength = MarkdownBlockPartitioner.GetStablePrefixLength(markdown);
        }

        RemoveTailElement();

        if (nextStableLength > stableLength)
        {
            AppendStableMarkdown(markdown[stableLength..nextStableLength]);
            stableLength = nextStableLength;
        }

        RenderTailMarkdown(markdown[stableLength..]);
    }

    private void AppendStableMarkdown(string markdown)
    {
        if (markdown.Length == 0)
        {
            return;
        }

        if (stableChunks.Count > 0 && !MarkdownBlockPartitioner.StartsWithHeading(markdown))
        {
            MarkdownRenderChunk lastChunk = stableChunks[^1];
            if (lastChunk.Markdown.Length + markdown.Length <= PreferredStableChunkLength)
            {
                UpdateStableChunk(lastChunk, lastChunk.Markdown + markdown);
                return;
            }
        }

        AddStableChunk(markdown);
    }

    private void AddStableChunk(string markdown)
    {
        UIElement element = MarkdownHelper.CreateMarkdownBlock(markdown);
        MarkdownRenderChunk chunk = new(markdown, element);
        stableChunks.Add(chunk);
        ContentHost.Children.Add(element);
    }

    private void UpdateStableChunk(MarkdownRenderChunk chunk, string markdown)
    {
        UIElement element = MarkdownHelper.CreateMarkdownBlock(markdown);
        int index = ContentHost.Children.IndexOf(chunk.Element);
        chunk.Markdown = markdown;

        if (index < 0)
        {
            chunk.Element = element;
            ContentHost.Children.Add(element);
            return;
        }

        ContentHost.Children.RemoveAt(index);
        ContentHost.Children.Insert(index, element);
        chunk.Element = element;
    }

    private void RenderTailMarkdown(string markdown)
    {
        if (markdown.Length == 0)
        {
            return;
        }

        tailElement = MarkdownHelper.CreateMarkdownBlock(markdown);
        ContentHost.Children.Add(tailElement);
    }

    private void RemoveTailElement()
    {
        if (tailElement is null)
        {
            return;
        }

        ContentHost.Children.Remove(tailElement);
        tailElement = null;
    }

    private sealed class MarkdownRenderChunk
    {
        public MarkdownRenderChunk(string markdown, UIElement element)
        {
            Markdown = markdown;
            Element = element;
        }

        public string Markdown { get; set; }

        public UIElement Element { get; set; }
    }
}
