using System.Collections.Generic;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableFunctionCallContent(SynchronizationContext synchronizationContext)
    : ObservableToolCallContent(synchronizationContext)
{
    public string Name { get; set; }

    public IDictionary<string, object?>? Arguments { get; set; }

    public Exception? Exception { get; set; }

    public bool InformationalOnly { get; set; }
}
