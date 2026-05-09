using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Snap.Nicole.Native;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.UI.Xaml;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static Snap.Nicole.Native.ConstValues;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowSubclassLifeTime : IDisposable
{
    private readonly Window window;
    private GCHandle<WindowSubclassLifeTime> gcHandle;
    private NicoleNativeWindowSubclass? subclass;

    private bool disposed;

    public unsafe WindowSubclassLifeTime(Window window)
    {
        this.window = window;
        gcHandle = new(this);
        subclass = NicoleNative.Default.MakeWindowSubclass(window.WindowHandle, NicoleNativeWindowSubclass.Callback.Create(&WindowSubclassCallback), gcHandle);
        subclass.Attach();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe BOOL WindowSubclassCallback(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, GCHandle<WindowSubclassLifeTime> userData, LRESULT* result)
    {
        WindowSubclassLifeTime? lifeTime = userData.Target;
        switch (uMsg)
        {
            case WM_NCRBUTTONDOWN:
            case WM_NCRBUTTONUP:
                return BOOL.FALSE;

            case WM_NCLBUTTONDBLCLK:
                {
                    if (lifeTime.window.AppWindow.Presenter is OverlappedPresenter { IsMaximizable: false })
                    {
                        return BOOL.FALSE;
                    }

                    break;
                }

            case WM_ERASEBKGND:
                {
                    if (lifeTime.window is IXamlWindowEraseBackground)
                    {
                        *result = BOOL.TRUE;
                        return BOOL.FALSE;
                    }

                    break;
                }
        }

        return BOOL.TRUE;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        subclass?.Detach();
        subclass = null;

        gcHandle.Dispose();
    }
}
