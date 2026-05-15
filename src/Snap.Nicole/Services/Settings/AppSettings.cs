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
    public string Language
    {
        get;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && SetProperty(ref field, value))
            {
                StringResourceProxy.Default.CurrentCulture = CultureInfo.GetCultureInfo(value);
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
}
