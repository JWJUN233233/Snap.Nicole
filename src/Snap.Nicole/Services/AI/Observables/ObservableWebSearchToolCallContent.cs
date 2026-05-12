using System.Collections.ObjectModel;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableWebSearchToolCallContent(SynchronizationContext synchronizationContext)
    : ObservableToolCallContent(synchronizationContext)
{
    public ObservableCollection<string>? Queries { get; set; }
}
