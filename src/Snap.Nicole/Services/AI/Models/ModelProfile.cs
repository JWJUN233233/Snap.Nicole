using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Nicole.Core;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.AI.Models;

[GeneratedCopyFrom<ModelProfile>]
internal sealed partial class ModelProfile : ObservableObject, IIdentifiable<Guid>
{
    [ObservableProperty]
    public partial Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonConverter(typeof(JsonStringEnumConverter<ModelProviderType>))]
    public partial ModelProviderType ProviderType { get; set; } = ModelProviderType.OpenAIChatCompletion;

    [ObservableProperty]
    public partial string Endpoint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ApiKey { get; set; }

    [ObservableProperty]
    public partial string ModelId { get; set; } = string.Empty;
}
