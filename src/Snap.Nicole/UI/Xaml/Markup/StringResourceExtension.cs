using Microsoft.UI.Xaml.Markup;
using Snap.Nicole.Resources;
using System.Globalization;

namespace Snap.Nicole.UI.Xaml.Markup;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
internal sealed class StringResourceExtension : MarkupExtension
{
    public SRName Name { get; set; }

    public string? CultureName { get; set; }

    protected override object ProvideValue()
    {
        CultureInfo cultureInfo = CultureName is not null ? CultureInfo.GetCultureInfo(CultureName) : CultureInfo.CurrentCulture;
        return SR.GetString(string.Intern(Name.ToString()), cultureInfo) ?? string.Empty;
    }
}
