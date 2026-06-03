using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using System.Threading;

namespace Snap.Nicole.UI.Xaml.Behaviors;

internal sealed partial class DisposeDataContextOnUnloadedBehavior : BehaviorBase<FrameworkElement>
{
    private bool disposed;

    protected override void OnAssociatedObjectUnloaded()
    {
        Cleanup();
    }

    protected override bool Uninitialize()
    {
        Cleanup();
        return base.Uninitialize();
    }

    private void Cleanup()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        if (AssociatedObject == null)
        {
            return;
        }

        object? dataContext = AssociatedObject.DataContext;
        AssociatedObject.DataContext = null;

        if (dataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
