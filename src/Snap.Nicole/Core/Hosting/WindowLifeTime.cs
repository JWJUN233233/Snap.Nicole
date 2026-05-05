using Microsoft.UI.Xaml;
using Snap.Nicole.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowLifeTime<TWindow>(IServiceProvider serviceProvider) : IWindowLifeTime<TWindow>
    where TWindow : Window
{
    private WindowSubclassLifeTime? subclass;

    public TWindow? Window { get; private set; }

    public void Show()
    {
        if (Window == null)
        {
            TWindow window = serviceProvider.GetRequiredService<TWindow>();
            Window = window;

            subclass = new WindowSubclassLifeTime(window);

            window.EnablePlacementRestoration(MemoryMarshal.AsRef<Guid>(CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(TypeNameHelper.GetTypeDisplayName(window)))));

            window.Closed += OnWindowClose;
        }

        Window.Activate();
    }

    public void Close()
    {
        Window?.Close();
    }

    private void OnWindowClose(object ignore, WindowEventArgs args)
    {
        if (Window is IXamlWindowCloseHandler handler)
        {
            handler.OnWindowClosing(out bool cancel);
            if (cancel)
            {
                args.Handled = true;
                return;
            }
        }

        // 1. Free subclass lifetime
        subclass?.Dispose();
        subclass = null;

        // 2. Clear window reference
        Window = null;
    }
}
