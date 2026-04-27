using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Snap.Nicole.Resources;

namespace Snap.Nicole.UI.Xaml.Markup;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
internal sealed class StringResourceExtension : MarkupExtension
{
    public SRName Name { get; set; }

    protected override object ProvideValue()
    {
        return new Binding
        {
            Source = StringResourceProxy.Default,
            Path = new PropertyPath($"[{Name}]"),
            Mode = BindingMode.OneWay,
        };
    }
}
