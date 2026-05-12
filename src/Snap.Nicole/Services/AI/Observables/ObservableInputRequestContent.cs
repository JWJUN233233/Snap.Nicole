using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal class ObservableInputRequestContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public string RequestId { get; set; }
}
