using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableWebSearchToolResultContent(SynchronizationContext synchronizationContext)
    : ObservableToolResultContent(synchronizationContext)
{
    public ObservableAIContentCollection? Outputs { get; set; }
}