using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableChatMessage(SynchronizationContext synchronizationContext) : INotifyPropertyChanged
{
    private readonly SynchronizationContext synchronizationContext = synchronizationContext;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? AuthorName { get; set => SetProperty(ref field, value); }

    public DateTimeOffset? CreatedAt { get; set => SetProperty(ref field, value); }

    public ChatRole Role { get; set => SetProperty(ref field, value); }

    public ObservableAIContentCollection Contents { get; set => SetProperty(ref field, value); } = [with(synchronizationContext)];

    public string? MessageId { get; set => SetProperty(ref field, value); }

    public object? RawRepresentation { get; set; }

    private bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        field = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = default)
    {
        synchronizationContext.Send(static state =>
        {
            if (state is not (ObservableChatMessage self, string propertyName))
            {
                return;
            }

            self.PropertyChanged?.Invoke(self, new(propertyName));
        }, (this, propertyName));
    }
}
