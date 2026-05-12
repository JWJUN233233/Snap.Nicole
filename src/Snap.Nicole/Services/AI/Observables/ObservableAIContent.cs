using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal class ObservableAIContent(SynchronizationContext synchronizationContext) : INotifyPropertyChanged
{
    private readonly SynchronizationContext synchronizationContext = synchronizationContext;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? RawRepresentation { get; set; }

    protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        field = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        synchronizationContext.Send(static state =>
        {
            if (state is not (ObservableAIContent self, string propertyName))
            {
                return;
            }

            self.PropertyChanged?.Invoke(self, new(propertyName));
        }, (this, propertyName));
    }
}
