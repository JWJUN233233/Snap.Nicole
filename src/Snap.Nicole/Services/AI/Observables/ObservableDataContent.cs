using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableDataContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext);
