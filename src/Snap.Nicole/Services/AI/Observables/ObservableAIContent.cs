using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal class ObservableAIContent : ObservableObject
{
    public object? RawRepresentation { get; set; }
}
