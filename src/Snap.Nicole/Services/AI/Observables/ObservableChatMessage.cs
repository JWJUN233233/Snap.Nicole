using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableChatMessage : ObservableObject
{
    [ObservableProperty]
    public partial string? AuthorName { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? CreatedAt { get; set; }

    [ObservableProperty]
    public partial ChatRole Role { get; set; }

    [ObservableProperty]
    public partial ObservableAIContentCollection Contents { get; set; } = new();

    [ObservableProperty]
    public partial string? MessageId { get; set; }

    public object? RawRepresentation { get; set; }
}
