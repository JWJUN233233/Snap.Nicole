using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Snap.Nicole.ViewModels;

internal sealed partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IOptionsProvider<AppSettings> options;
    private readonly IDisposable? changeRegistration;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        options = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>();

        ModelProfiles = new OptionsObservableCollection<AppSettings, ModelProfile, Guid>(options, settings => settings.ModelProfiles, (settings, items) => settings.ModelProfiles = items);

        changeRegistration = options.OnChange(OnSettingsChanged);
    }

    // TODO: Potentially cache this list
    public IReadOnlyList<SettingsItem<string>> Languages { get; } = [.. StringResourceProxy.SupportedCultures.Select(name => new SettingsItem<string>(CultureInfo.GetCultureInfo(name).NativeName, name))];

    public string Language
    {
        get => options.CurrentValue.Language;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(options.CurrentValue.Language, value, StringComparison.Ordinal))
            {
                return;
            }

            options.CurrentValue.Language = value;
            options.Update();
        }
    }

    public OptionsObservableCollection<AppSettings, ModelProfile, Guid> ModelProfiles { get; private set; } 

    [ObservableProperty]
    public partial ModelProfile? SelectedProfile { get; set; }

    [RelayCommand]
    private void AddProfile()
    {
        ModelProfile profile = new();

        ModelProfiles.Add(profile);
        if (ModelProfiles.Count == 1)
        {
            options.CurrentValue.SelectedModelProfileId = profile.Id;
        }

        SelectedProfile = profile;
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is not { } selected)
        {
            return;
        }

        if (options.CurrentValue.SelectedModelProfileId == selected.Id)
        {
            options.CurrentValue.SelectedModelProfileId = ModelProfiles.FirstOrDefault(p => p != selected)?.Id;
        }

        ModelProfiles.Remove(selected);
        SelectedProfile = null;
    }

    public void Dispose()
    {
        changeRegistration?.Dispose();
    }

    private void OnSettingsChanged(AppSettings ignored, string? ignored2)
    {
        App.Current.Threading.SynchronizationContext.Post(static state =>
        {
            if (state is not SettingsViewModel self)
            {
                return;
            }

            self.OnPropertyChanged(nameof(Language));
            self.ModelProfiles.Update(self.options.CurrentValue.ModelProfiles);
        }, this);
    }
}
