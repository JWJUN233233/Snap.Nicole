using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Snap.Nicole.ViewModels;

internal sealed partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;
    }

    public AppSettings Settings { get; }

    // TODO: Potentially cache this list
    public IReadOnlyList<SettingsItem<string>> Languages { get; } = [.. StringResourceProxy.SupportedCultures.Select(name => new SettingsItem<string>(CultureInfo.GetCultureInfo(name).NativeName, name))];

    public IReadOnlyList<SettingsItem<ModelProviderType>> ModelProviderTypes { get; } =
    [
        new("OpenAI Chat Completion", ModelProviderType.OpenAIChatCompletion),
        new("OpenAI Responses", ModelProviderType.OpenAIResponses),
        new("Anthropic", ModelProviderType.Anthropic),
    ];

    [RelayCommand]
    private void AddProfile()
    {
        ModelProfile profile = new();

        Settings.ModelProfiles.Add(profile);
        Settings.ModelProfiles.CurrentItem = profile;
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (Settings.ModelProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        Settings.ModelProfiles.Remove(selected);
    }
}
