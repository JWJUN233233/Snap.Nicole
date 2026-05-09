using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Models;

internal sealed partial class ModelProfile : ObservableObject
{
    [ObservableProperty]
    public partial Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Endpoint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ApiKey { get; set; }

    [ObservableProperty]
    public partial string ModelId { get; set; } = string.Empty;
}
