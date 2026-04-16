using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.UI.Xaml;

namespace Snap.Nicole.Core.DependencyInjection;

internal sealed class XamlWindowBuilder(IServiceCollection services) : IXamlWindowBuilder
{
    public IXamlWindowBuilder AddXamlWindow<TWindow>()
        where TWindow : Window
    {
        services.TryAddTransient<TWindow>();
        return this;
    }
}
