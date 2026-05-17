using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Snap.Nicole.Resources;

namespace Snap.Nicole.UI.Xaml.Markup;

[MarkupExtensionReturnType(ReturnType = typeof(Binding))]
internal sealed class StringResourceExtension : MarkupExtension
{
    public SRName Name { get; set; }

    protected override object ProvideValue()
    {
        return StringResourceProxy.Default.CreateBinding($"{Name}");
    }

    protected override object ProvideValue(IXamlServiceProvider serviceProvider)
    {
        return ProvideValue();
    }
}
