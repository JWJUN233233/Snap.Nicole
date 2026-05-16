using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed partial class ChatPlainTextSegmentView : UserControl
{
    public ChatPlainTextSegmentView()
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
        typeof(ChatPlainTextSegmentView),
        new PropertyMetadata(null, OnTitleChanged));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ChatPlainTextSegmentView),
        new PropertyMetadata(null));

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChatPlainTextSegmentView view)
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
