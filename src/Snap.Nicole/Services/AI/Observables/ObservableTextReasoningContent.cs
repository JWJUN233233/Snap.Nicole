using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableTextReasoningContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public string Text { get; set; }
}
