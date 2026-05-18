using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Nicole.Core;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Snap.Nicole.ViewModels.Settings;

internal sealed partial class SettingsViewModel(IServiceProvider serviceProvider) : ObservableObject
{
    public AppSettings Settings { get; } = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

    public SettingsGitSyncViewModel GitSync { get; } = serviceProvider.GetRequiredService<SettingsGitSyncViewModel>();

    // TODO: Potentially cache this list
    public IReadOnlyList<SettingsItem<string>> Languages { get; } = [.. StringResourceProxy.SupportedCultures.Select(name => new SettingsItem<string>(CultureInfo.GetCultureInfo(name).NativeName, name))];

    public IReadOnlyList<SettingsItem<EnumBox<ModelProviderType>>> ModelProviderTypes { get; } =
    [
        new("OpenAI Chat Completions | ~/chat/completions", EnumBox.Of(ModelProviderType.OpenAIChatCompletion)),
        new("OpenAI Responses | ~/responses", EnumBox.Of(ModelProviderType.OpenAIResponses)),
        new("Anthropic Messages | ~/v1/messages", EnumBox.Of(ModelProviderType.Anthropic)),
    ];

    [RelayCommand]
    private void AddProfile()
    {
        ModelProviderProfile providerProfile = new();

        Settings.ModelProviderProfiles.Add(providerProfile);
        Settings.ModelProviderProfiles.CurrentItem = providerProfile;
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        Settings.ModelProviderProfiles.Remove(selected);
    }
}
