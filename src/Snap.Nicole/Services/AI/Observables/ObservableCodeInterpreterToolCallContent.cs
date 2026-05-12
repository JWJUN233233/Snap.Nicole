using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableCodeInterpreterToolCallContent(SynchronizationContext synchronizationContext)
    : ObservableToolCallContent(synchronizationContext)
{
    public ObservableAIContentCollection? Inputs { get; set; }
}
