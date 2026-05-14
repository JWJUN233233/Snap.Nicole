using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace Snap.Nicole.Services.Settings;

internal interface IOptionsProvider<out T> : IOptionsMonitor<T>
    where T : class, INotifyPropertyChanged;
