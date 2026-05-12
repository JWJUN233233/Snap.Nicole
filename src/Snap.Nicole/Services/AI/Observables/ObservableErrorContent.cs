using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableErrorContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public string Message { get; set; }

    public string? ErrorCode { get; set; }

    public string? Details { get; set; }
}
