using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Nicole.Core;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Snap.Nicole.Services.Settings;

[GeneratedCopyFrom<AppSettings>]
internal sealed partial class AppSettings : ObservableObject, ICopyFrom<AppSettings>, IOptionsObservableChildrenProvider
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

    public ObservableSettingsCollection<ModelProviderProfile, Guid> ModelProviderProfiles { get; set => SetProperty(ref field, value ?? []); } = [];

    public Guid? SelectedModelProviderProfileId
    {
        get => ModelProviderProfiles.CurrentItemId;
        set => ModelProviderProfiles.CurrentItemId = value;
    }

    public IEnumerable<INotifyPropertyChanged> EnumerateObservableChildren()
    {
        return ModelProviderProfiles.EnumerateObservableChildren();
    }
}
