using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Nicole.Core;
using Snap.Nicole.Services.Settings;

namespace Snap.Nicole.Services.AI.Models;

internal sealed partial class ModelProviderProfile : ObservableObject, IIdentifiable<Guid>, ICopyFrom<ModelProviderProfile>
{
    [ObservableProperty]
    public partial Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EnumBox<ModelProviderType> ProviderType { get; set; } = EnumBox.Of(ModelProviderType.OpenAIChatCompletion);

    [ObservableProperty]
    public partial string Endpoint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ApiKey { get; set; }

    [ObservableProperty]
    public partial string? ModelListDocumentationLink { get; set; }

    public ObservableSettingsCollection<ModelProfile, Guid> ModelProfiles { get; set => SetProperty(ref field, value ?? []); } = [];

    public Guid? SelectedModelProfileId
    {
        get => ModelProfiles.CurrentItemId;
        set => ModelProfiles.CurrentItemId = value;
    }

    public void CopyFrom(ModelProviderProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Id = source.Id;
        Name = source.Name;
        ProviderType = source.ProviderType;
        Endpoint = source.Endpoint;
        ApiKey = source.ApiKey;
        ModelListDocumentationLink = source.ModelListDocumentationLink;
        ModelProfiles.CopyFrom(source.ModelProfiles);
        SelectedModelProfileId = source.SelectedModelProfileId;
    }
}
