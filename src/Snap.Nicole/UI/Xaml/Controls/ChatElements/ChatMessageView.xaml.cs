using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Services.AI.Observables;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

[GeneratedDependencyProperty<ObservableChatMessage>("Message")]
internal sealed partial class ChatMessageView : UserControl
{
    public ChatMessageView()
    {
        InitializeComponent();
    }
}
