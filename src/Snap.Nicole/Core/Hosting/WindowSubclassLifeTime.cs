using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.UI.Xaml;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Snap.Nicole.Native.ConstValues;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowSubclassLifeTime(Window currentWindow)
{
    private Window CurrentWindow { get; } = currentWindow;

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
                    if (lifeTime.CurrentWindow.AppWindow.Presenter is OverlappedPresenter { IsMaximizable: false })
                    {
                        return BOOL.FALSE;
                    }

                    break;
                }

            case WM_ERASEBKGND:
                {
                    if (lifeTime.CurrentWindow is IXamlWindowEraseBackground)
                    {
                        *result = BOOL.TRUE;
                        return BOOL.FALSE;
                    }

                    break;
                }

            case WM_MOUSEWHEEL:
                {
                    //if (lifeTime.CurrentWindow is IXamlWindowMouseWheelHandler handler)
                    //{
                    //    WPARAM2MOUSEWHEEL pWParam2 = *(WPARAM2MOUSEWHEEL*)&wParam;
                    //    LPARAM2MOUSEWHEEL pLParam2 = *(LPARAM2MOUSEWHEEL*)&lParam;
                    //    PointerPointProperties data = new(pWParam2.High, (MODIFIERKEYS_FLAGS)pWParam2.Low, pLParam2.Low, pLParam2.High);
                    //    handler.OnMouseWheel(in data);
                    //}

                    break;
                }
        }

        return BOOL.TRUE;
    }
}