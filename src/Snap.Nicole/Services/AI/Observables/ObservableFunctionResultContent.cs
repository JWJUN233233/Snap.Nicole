using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableFunctionResultContent(SynchronizationContext synchronizationContext)
    : ObservableToolResultContent(synchronizationContext)
{
    public object? Result { get; set; }

    public Exception? Exception { get; set; }
}
