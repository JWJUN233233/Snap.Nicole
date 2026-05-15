using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.UI.Xaml.Helpers;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed partial class MarkdownTextBlock : UserControl
{
    public MarkdownTextBlock()
    {
        InitializeComponent();
        RenderMarkdown();
    }

    public string? Markdown
    {
        get => (string?)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown),
        typeof(string),
        typeof(MarkdownTextBlock),
        new PropertyMetadata(null, OnMarkdownChanged));

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock view)
        {
            view.RenderMarkdown();
        }
    }

    private void RenderMarkdown()
    {
        ContentHost.Content = MarkdownHelper.CreateMarkdownBlock(Markdown);
    }
}
