using Microsoft.UI;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.Native.UI.Shell;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal static unsafe class WindowUtilities
{
    public const string IID_ITaskbarList3 = "EA1AFB91-9E28-4B86-90E9-9E9F8A5EEA84";

    public static void AppWindowEnablePlacementRestoration(WindowId windowId, Guid guid)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(WindowId windowId, Guid guid);

        Marshal.ThrowExceptionForHR(NativeMethod(windowId, guid));
    }

    public static void SwitchToWindow(HWND hWnd)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd));
    }

    public static void AddExtendedStyleLayered(HWND hWnd)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd));
    }

    public static void RemoveExtendedStyleLayered(HWND hWnd)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd));
    }

    public static void SetLayeredWindowTransparency(HWND hWnd, byte opacity)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd, byte opacity);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd, opacity));
    }

    public static void AddExtendedStyleToolWindow(HWND hWnd)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd));
    }

    public static void RemoveStyleOverlappedWindow(HWND hWnd)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd));
    }

    public static float GetRasterizationScaleForWindow(HWND hWnd)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd, float* scale);

        float scale;
        Marshal.ThrowExceptionForHR(NativeMethod(hWnd, &scale));
        return scale;
    }

    public static void SetWindowIsEnabled(HWND hWnd, BOOL isEnabled)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd, BOOL isEnabled);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd, isEnabled));
    }

    public static void SetWindowOwner(HWND hWnd, HWND hWndOwner)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd, HWND hWndOwner);

        Marshal.ThrowExceptionForHR(NativeMethod(hWnd, hWndOwner));
    }

    public static void SetTaskbarProgress(HWND hWnd, TBPFLAG state, ulong value, ulong maximum, ref ObjectReference<IUnknownVftbl>? taskbar)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(HWND hWnd, TBPFLAG state, ulong value, ulong maximum, IUnknownVftbl** ppTaskbar);

        nint pv = MarshalInterfaceHelper<object>.GetAbi(taskbar);
        Marshal.ThrowExceptionForHR(NativeMethod(hWnd, state, value, maximum, (IUnknownVftbl**)&pv));
        taskbar = pv == 0 ? null : ObjectReference<IUnknownVftbl>.Attach(ref pv, new(IID_ITaskbarList3));
    }
}
