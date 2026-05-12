using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableCodeInterpreterToolResultContent(SynchronizationContext synchronizationContext)
    : ObservableToolResultContent(synchronizationContext)
{
    public ObservableAIContentCollection? Outputs { get; set; }
}
