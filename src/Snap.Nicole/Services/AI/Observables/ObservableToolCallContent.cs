using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal class ObservableToolCallContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public string CallId { get; set; }
}
