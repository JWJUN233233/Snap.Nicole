using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.Globalization;
using System.Globalization;

namespace Snap.Nicole.Resources;

internal sealed class StringResourceProxy : ObservableObject
{
    public static StringResourceProxy Default { get; } = new();

    public CultureInfo CurrentCulture
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;

                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;

                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;

                ApplicationLanguages.PrimaryLanguageOverride = value.Name;
                OnPropertyChanged("Item[]");
            }
        }
    } = CultureInfo.CurrentCulture;

    public string this[string name]
    {
        get => SR.GetString(string.Intern(name), CurrentCulture) ?? string.Empty;
    }
}
