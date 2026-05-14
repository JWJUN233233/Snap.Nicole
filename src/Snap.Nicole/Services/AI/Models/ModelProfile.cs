using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Nicole.Core;

namespace Snap.Nicole.Services.AI.Models;

[GeneratedCopyFrom]
internal sealed partial class ModelProfile : ObservableObject, IIdentifiable<Guid>, ICopyFrom<ModelProfile>
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
