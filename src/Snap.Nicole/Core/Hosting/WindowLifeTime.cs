using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowLifeTime<TWindow>(IServiceProvider serviceProvider) : IWindowLifeTime<TWindow>
    where TWindow : Window
{
    private TWindow? currentWindow;

    public void Show()
    {
        if (currentWindow != null)
        {
            currentWindow.Activate();
            return;
        }

        currentWindow = serviceProvider.GetRequiredService<TWindow>();
        currentWindow.Closed += OnWindowClose;
        currentWindow.Activate();
    }

    public void Close()
    {
        currentWindow?.Close();
    }

    private void OnWindowClose(object ignore, WindowEventArgs args)
    {
        currentWindow = null;
    }
}
