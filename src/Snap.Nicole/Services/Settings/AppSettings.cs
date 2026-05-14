using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Nicole.Core;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Snap.Nicole.Services.Settings;

internal sealed class AppSettings : ObservableObject, ICopyFrom<AppSettings>, IOptionsChangeSourceProvider
{
    public AppSettings()
    {
        ApplyLanguage(Language);
    }

    public string Language
    {
        get;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && SetProperty(ref field, value))
            {
                ApplyLanguage(value);
            }
        }
    } = StringResourceProxy.SupportedCultures[0];

    public ObservableSettingsCollection<ModelProfile, Guid> ModelProfiles { get; set => SetProperty(ref field, value ?? []); } = [];

    public Guid? SelectedModelProfileId
    {
        get => ModelProfiles.CurrentItemId;
        set => ModelProfiles.CurrentItemId = value;
    }

    public void CopyFrom(AppSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Language = source.Language;
        ModelProfiles.CopyFrom(source.ModelProfiles);
        SelectedModelProfileId = source.SelectedModelProfileId;
    }

    public IEnumerable<INotifyPropertyChanged> GetChangeSources()
    {
        yield return ModelProfiles;
    }

    private static void ApplyLanguage(string language)
    {
        if (Microsoft.UI.Xaml.Application.Current is App app)
        {
            app.Threading.SynchronizationContext.Post(static state =>
            {
                if (state is string current)
                {
                    ApplyLanguageCore(current);
                }
            }, language);

            return;
        }

        ApplyLanguageCore(language);
    }

    private static void ApplyLanguageCore(string language)
    {
        StringResourceProxy.Default.CurrentCulture = CultureInfo.GetCultureInfo(language);
    }
}
