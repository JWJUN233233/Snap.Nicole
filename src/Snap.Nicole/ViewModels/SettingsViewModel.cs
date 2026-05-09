using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Snap.Nicole.ViewModels;

internal sealed partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IOptionsMonitor<AppSettings> monitor;
    private readonly IOptionsWriter<AppSettings> writer;
    private readonly IDisposable? changeRegistration;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        monitor = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
        writer = serviceProvider.GetRequiredService<IOptionsWriter<AppSettings>>();
        changeRegistration = monitor.OnChange(OnSettingsChanged);
    }

    public IReadOnlyList<SettingsItem<string>> Languages { get; } = [.. StringResourceProxy.SupportedCultures.Select(name => new SettingsItem<string>(CultureInfo.GetCultureInfo(name).NativeName, name))];

    public string Language
    {
        get => monitor.CurrentValue.Language;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(monitor.CurrentValue.Language, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            monitor.CurrentValue.Language = value;
            writer.Update();
        }
    }

    public ObservableCollection<ModelProfile> ModelProfiles => new(monitor.CurrentValue.ModelProfiles);

    [ObservableProperty]
    public partial ModelProfile? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial string ProfileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProfileEndpoint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProfileApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProfileModelId { get; set; } = string.Empty;

    partial void OnSelectedProfileChanged(ModelProfile? value)
    {
        if (value is not null)
        {
            ProfileName = value.Name;
            ProfileEndpoint = value.Endpoint;
            ProfileApiKey = value.ApiKey ?? "";
            ProfileModelId = value.ModelId;
        }
        else
        {
            ProfileName = string.Empty;
            ProfileEndpoint = string.Empty;
            ProfileApiKey = string.Empty;
            ProfileModelId = string.Empty;
        }
    }

    [RelayCommand]
    private void AddProfile()
    {
        string name = string.IsNullOrWhiteSpace(ProfileName) ? $"Profile {monitor.CurrentValue.ModelProfiles.Count + 1}" : ProfileName;
        ModelProfile profile = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Endpoint = ProfileEndpoint,
            ApiKey = string.IsNullOrWhiteSpace(ProfileApiKey) ? null : ProfileApiKey,
            ModelId = string.IsNullOrWhiteSpace(ProfileModelId) ? "gpt-4o" : ProfileModelId,
        };

        monitor.CurrentValue.ModelProfiles.Add(profile);
        if (monitor.CurrentValue.ModelProfiles.Count == 1)
        {
            monitor.CurrentValue.SelectedModelProfileId = profile.Id;
        }

        SelectedProfile = profile;
        writer.Update();
        OnPropertyChanged(nameof(ModelProfiles));
    }

    [RelayCommand]
    private void UpdateProfile()
    {
        ModelProfile? selected = SelectedProfile;
        if (selected is null)
        {
            return;
        }

        selected.Name = string.IsNullOrWhiteSpace(ProfileName) ? selected.Name : ProfileName;
        selected.Endpoint = ProfileEndpoint;
        selected.ApiKey = string.IsNullOrWhiteSpace(ProfileApiKey) ? null : ProfileApiKey;
        selected.ModelId = string.IsNullOrWhiteSpace(ProfileModelId) ? selected.ModelId : ProfileModelId;

        writer.Update();
        OnPropertyChanged(nameof(ModelProfiles));
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        ModelProfile? selected = SelectedProfile;
        if (selected is null)
        {
            return;
        }

        monitor.CurrentValue.ModelProfiles.Remove(selected);
        if (monitor.CurrentValue.SelectedModelProfileId == selected.Id)
        {
            monitor.CurrentValue.SelectedModelProfileId = monitor.CurrentValue.ModelProfiles.FirstOrDefault()?.Id;
        }

        writer.Update();
        SelectedProfile = null;
        OnPropertyChanged(nameof(ModelProfiles));
    }

    public void Dispose()
    {
        changeRegistration?.Dispose();
    }

    private void OnSettingsChanged(AppSettings ignored)
    {
        App.Current.Threading.SynchronizationContext.Post(static state =>
        {
            if (state is not SettingsViewModel self)
            {
                return;
            }

            self.OnPropertyChanged(nameof(Language));
            self.OnPropertyChanged(nameof(ModelProfiles));
        }, this);
    }
}
