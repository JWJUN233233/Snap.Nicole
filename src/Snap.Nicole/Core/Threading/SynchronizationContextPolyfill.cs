using Microsoft.UI.Dispatching;
using System.Threading;
using WinRT;

namespace Snap.Nicole.Core.Threading;

internal sealed class SynchronizationContextPolyfill(DispatcherQueue dispatcherQueue) : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                callback(state);
            }
            catch (Exception ex)
            {
                ExceptionHelpers.ReportUnhandledError(ex);
            }
        });
    }

    public override void Send(SendOrPostCallback callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        dispatcherQueue.Invoke(() =>
        {
            try
            {
                callback(state);
            }
            catch (Exception ex)
            {
                ExceptionHelpers.ReportUnhandledError(ex);
            }
        });
    }

    public override SynchronizationContext CreateCopy()
    {
        return new SynchronizationContextPolyfill(dispatcherQueue);
    }
}
