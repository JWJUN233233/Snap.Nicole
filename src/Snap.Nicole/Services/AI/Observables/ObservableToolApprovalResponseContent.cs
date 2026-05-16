using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableToolApprovalResponseContent : ObservableInputResponseContent
{
    [ObservableProperty]
    public partial bool Approved { get; set; }

    [ObservableProperty]
    public partial ObservableToolCallContent ToolCall { get; set; }

    [ObservableProperty]
    public partial string? Reason { get; set; }
}
