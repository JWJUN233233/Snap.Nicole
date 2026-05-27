using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

[GeneratedDependencyProperty<object>("Value", PropertyChangedCallbackName = nameof(OnValueChanged))]
internal sealed partial class ChatObjectSegmentView : UserControl
{
    public ChatObjectSegmentView()
    {
        InitializeComponent();
        UpdateText();
    }

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
