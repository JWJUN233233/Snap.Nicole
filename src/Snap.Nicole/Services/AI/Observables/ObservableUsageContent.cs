using Microsoft.Extensions.AI;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableUsageContent(SynchronizationContext synchronizationContext)
    : ObservableAIContent(synchronizationContext)
{
    public UsageDetails Details { get; set; }
}
