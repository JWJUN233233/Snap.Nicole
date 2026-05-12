using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableToolApprovalResponseContent(SynchronizationContext synchronizationContext)
    : ObservableInputResponseContent(synchronizationContext)
{
    public bool Approved { get; set; }

    public ObservableToolCallContent ToolCall { get; set; }

    public string? Reason { get; set; }
}
