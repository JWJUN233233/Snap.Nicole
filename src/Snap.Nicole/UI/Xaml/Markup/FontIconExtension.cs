using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Snap.Nicole.UI.Xaml.Markup;

[MarkupExtensionReturnType(ReturnType = typeof(FontIcon))]
internal sealed class FontIconExtension : MarkupExtension
{
    public string Glyph { get; set; } = default!;

    public double FontSize { get; set; } = 12;

    protected override object ProvideValue()
    {
        return new FontIcon
        {
            Glyph = Glyph,
            FontSize = FontSize,
        };
    }
}
