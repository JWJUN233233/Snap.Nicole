using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableToolApprovalRequestContent(SynchronizationContext synchronizationContext)
    : ObservableInputRequestContent(synchronizationContext)
{
    public ObservableToolCallContent ToolCall { get; set; }
}
