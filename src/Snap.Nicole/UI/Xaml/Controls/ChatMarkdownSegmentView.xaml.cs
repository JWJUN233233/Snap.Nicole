using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Nicole.UI.Xaml.Controls;

internal sealed partial class ChatMarkdownSegmentView : UserControl
{
    public ChatMarkdownSegmentView()
    {
        InitializeComponent();
        UpdateTitleVisibility();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(ChatMarkdownSegmentView),
        new PropertyMetadata(null, OnTitleChanged));

    public string? Markdown
    {
        get => (string?)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown),
        typeof(string),
        typeof(ChatMarkdownSegmentView),
        new PropertyMetadata(null));

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChatMarkdownSegmentView view)
        {
            view.UpdateTitleVisibility();
        }
    }

    private void UpdateTitleVisibility()
    {
        TitleTextBlock.Visibility = string.IsNullOrWhiteSpace(Title)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
