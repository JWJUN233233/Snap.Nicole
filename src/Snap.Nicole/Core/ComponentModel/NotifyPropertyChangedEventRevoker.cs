using System.ComponentModel;
using System.Threading;

namespace Snap.Nicole.Core.ComponentModel;

internal sealed class NotifyPropertyChangedEventRevoker : IDisposable
{
    private readonly INotifyPropertyChanged notifyPropertyChanged;
    private readonly PropertyChangedEventHandler handler;
    private bool disposed;

    public NotifyPropertyChangedEventRevoker(INotifyPropertyChanged notifyPropertyChanged, PropertyChangedEventHandler handler)
    {
        this.notifyPropertyChanged = notifyPropertyChanged;
        this.handler = handler;

        notifyPropertyChanged.PropertyChanged += handler;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        notifyPropertyChanged.PropertyChanged -= handler;
    }
}