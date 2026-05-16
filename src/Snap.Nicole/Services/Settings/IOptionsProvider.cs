using System.ComponentModel;

namespace Snap.Nicole.Services.Settings;

internal interface IOptionsProvider<out T>
    where T : class, INotifyPropertyChanged
{
    T CurrentValue { get; }
}
