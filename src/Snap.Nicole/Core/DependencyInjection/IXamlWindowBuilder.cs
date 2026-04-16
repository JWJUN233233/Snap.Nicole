using Microsoft.UI.Xaml;

namespace Snap.Nicole.Core.DependencyInjection;

internal interface IXamlWindowBuilder
{
    IXamlWindowBuilder AddXamlWindow<TWindow>()
        where TWindow : Window;
}