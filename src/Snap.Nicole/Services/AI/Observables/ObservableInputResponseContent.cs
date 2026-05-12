using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal class ObservableInputResponseContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public string RequestId { get; set; }
}
