using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Snap.Nicole.Native;
using Snap.Nicole.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowLifeTime<TWindow>(IServiceProvider serviceProvider) : IWindowLifeTime<TWindow>
    where TWindow : Window
{
    // All methods in this class must be called on the UI thread, so we don't need to consider thread safety issues.
    private TWindow? currentWindow;
    private NicoleNativeWindowSubclass? subclass;
    private GCHandle<WindowSubclassLifeTime> gcHandle;

    public unsafe void Show()
    {
        if (currentWindow != null)
        {
            currentWindow.Activate();
            return;
        }

        currentWindow = serviceProvider.GetRequiredService<TWindow>();

        gcHandle = new(new(currentWindow));
        subclass = NicoleNative.Default.MakeWindowSubclass(currentWindow.WindowHandle, NicoleNativeWindowSubclass.Callback.Create(&WindowSubclassLifeTime.WindowSubclassCallback), gcHandle);
        subclass.Attach();

        string windowName = TypeNameHelper.GetTypeDisplayName(currentWindow);
        byte[] data = CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(windowName));
        currentWindow.AppWindow.PersistedStateId = MemoryMarshal.AsRef<Guid>(data);
        currentWindow.AppWindow.PlacementRestorationBehavior = PlacementRestorationBehavior.All;

        currentWindow.Closed += OnWindowClose;
        currentWindow.Activate();
    }

    public void Close()
    {
        currentWindow?.Close();
    }

    private void OnWindowClose(object ignore, WindowEventArgs args)
    {
        // TODO: support closing cancellation

        // 1. Detach subclass to avoid callback after window is closed
        subclass?.Detach();
        subclass = null;

        // 2. Free subclass lifetime
        gcHandle.Dispose();

        // 3. Clear window reference
        currentWindow = null;
    }
}
