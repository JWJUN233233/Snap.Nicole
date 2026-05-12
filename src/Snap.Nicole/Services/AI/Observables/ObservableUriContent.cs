using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableUriContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public Uri Uri { get; set; }

    public string MediaType { get; set; }
}
