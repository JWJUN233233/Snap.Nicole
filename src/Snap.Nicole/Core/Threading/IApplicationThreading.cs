using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Core.Threading;

internal interface IApplicationThreading
{
    SynchronizationContext SynchronizationContext { get; }

    TaskScheduler TaskScheduler { get; }
}
