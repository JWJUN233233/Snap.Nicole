using Microsoft.UI.Xaml;

namespace Snap.Nicole.Core.Hosting;

internal interface IWindowLifeTime<TWindow>
    where TWindow : Window
{
    void Show();

    void Close();
}