using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableToolApprovalRequestContent : ObservableInputRequestContent
{
    [ObservableProperty]
    public partial ObservableToolCallContent ToolCall { get; set; }
}
