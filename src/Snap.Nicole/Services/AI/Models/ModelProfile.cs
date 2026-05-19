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
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string ModelId { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return Name;
            }

            return ModelId;
        }
    }

    public static ModelProfile Create(string modelId)
    {
        return new ModelProfile
        {
            Name = modelId,
            ModelId = modelId,
        };
    }
}
