using System.Collections.Generic;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableMcpServerToolCallContent(SynchronizationContext synchronizationContext)
    : ObservableToolCallContent(synchronizationContext)
{
    public string Name { get; set; }

    public string? ServerName { get; set; }

    public IDictionary<string, object?>? Arguments { get; set; }
}
