using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableHostedFileContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext);
