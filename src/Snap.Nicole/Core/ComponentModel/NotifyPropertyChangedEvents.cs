using System.ComponentModel;

namespace Snap.Nicole.Core.ComponentModel;

internal static class NotifyPropertyChangedEvents
{
    public static NotifyPropertyChangedEventRevoker AutoRevoke(INotifyPropertyChanged notifyPropertyChanged, PropertyChangedEventHandler handler)
    {
        return new NotifyPropertyChangedEventRevoker(notifyPropertyChanged, handler);
    }
}
