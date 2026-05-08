using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.Settings;
using System;
using System.Collections.Generic;
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
        changeRegistration = monitor.OnChange((_) => OnSettingsChanged());
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

    public void Dispose()
    {
        changeRegistration?.Dispose();
    }

    public string OpenAIApiKey
    {
        get => monitor.CurrentValue.OpenAIApiKey ?? "";
        set
        {
            if (string.Equals(monitor.CurrentValue.OpenAIApiKey, value, StringComparison.Ordinal))
            {
                return;
            }

            monitor.CurrentValue.OpenAIApiKey = value;
            writer.Update();
        }
    }

    public string OpenAIBaseUrl
    {
        get => monitor.CurrentValue.OpenAIBaseUrl ?? "";
        set
        {
            if (string.Equals(monitor.CurrentValue.OpenAIBaseUrl, value, StringComparison.Ordinal))
            {
                return;
            }

            monitor.CurrentValue.OpenAIBaseUrl = value;
            writer.Update();
        }
    }

    public string DefaultModel
    {
        get => monitor.CurrentValue.DefaultModel;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(monitor.CurrentValue.DefaultModel, value, StringComparison.Ordinal))
            {
                return;
            }

            monitor.CurrentValue.DefaultModel = value;
            writer.Update();
        }
    }

    private void OnSettingsChanged()
    {
        App.Current.Threading.SynchronizationContext.Post(static state =>
        {
            if (state is not SettingsViewModel self)
            {
                return;
            }

            self.OnPropertyChanged(nameof(Language));
            self.OnPropertyChanged(nameof(OpenAIApiKey));
            self.OnPropertyChanged(nameof(OpenAIBaseUrl));
            self.OnPropertyChanged(nameof(DefaultModel));
        }, this);
    }
}
