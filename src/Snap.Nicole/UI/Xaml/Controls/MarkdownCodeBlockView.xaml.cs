using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Nicole.UI.Xaml.Controls;

internal sealed partial class MarkdownCodeBlockView : UserControl
{
    public MarkdownCodeBlockView()
    {
        InitializeComponent();
    }

    public string? Code
    {
        get => (string?)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code),
        typeof(string),
        typeof(MarkdownCodeBlockView),
        new PropertyMetadata(null));

    public string? Language
    {
        get => (string?)GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public static readonly DependencyProperty LanguageProperty = DependencyProperty.Register(
        nameof(Language),
        typeof(string),
        typeof(MarkdownCodeBlockView),
        new PropertyMetadata(null));
}
