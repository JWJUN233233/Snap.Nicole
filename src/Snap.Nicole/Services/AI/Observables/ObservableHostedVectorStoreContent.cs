using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableHostedVectorStoreContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext);
