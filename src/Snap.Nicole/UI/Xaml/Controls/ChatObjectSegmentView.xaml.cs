using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Nicole.UI.Xaml.Controls;

internal sealed partial class ChatObjectSegmentView : UserControl
{
    public ChatObjectSegmentView()
    {
        InitializeComponent();
        UpdateText();
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(object),
        typeof(ChatObjectSegmentView),
        new PropertyMetadata(null, OnValueChanged));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChatObjectSegmentView view)
        {
            view.UpdateText();
        }
    }

    private void UpdateText()
    {
        Segment.Text = Value?.ToString() ?? string.Empty;
    }
}
