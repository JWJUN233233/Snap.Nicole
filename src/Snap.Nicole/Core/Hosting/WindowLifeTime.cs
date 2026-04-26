using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Snap.Nicole.Native;
using Snap.Nicole.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowLifeTime<TWindow>(IServiceProvider serviceProvider) : IWindowLifeTime<TWindow>
    where TWindow : Window
{
    // All methods in this class must be called on the UI thread, so we don't need to consider thread safety issues.
    private NicoleNativeWindowSubclass? subclass;
    private GCHandle<IWindowSubclassLifeTime> gcHandle;

    public TWindow? Window { get; private set; }

    public unsafe void Show()
    {
        if (Window == null)
        {
            TWindow window = serviceProvider.GetRequiredService<TWindow>();
            Window = window;
            gcHandle = new(new WindowSubclassLifeTime(window));
            subclass = NicoleNative.Default.MakeWindowSubclass(window.WindowHandle, NicoleNativeWindowSubclass.Callback.Create(&WindowSubclassLifeTime.WindowSubclassCallback), gcHandle);
            subclass.Attach();

            AppWindow appWindow = window.AppWindow;
            appWindow.PersistedStateId = MemoryMarshal.AsRef<Guid>(CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(TypeNameHelper.GetTypeDisplayName(window))));
            appWindow.PlacementRestorationBehavior = PlacementRestorationBehavior.All;

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

        // 1. Detach subclass to avoid callback after window is closed
        subclass?.Detach();
        subclass = null;

        // 2. Free subclass lifetime
        gcHandle.Dispose();

        // 3. Clear window reference
        Window = null;
    }
}
