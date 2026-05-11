using Microsoft.UI;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.Native.UI.Shell;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal static unsafe class WindowUtilities
{
    public static void AppWindowEnablePlacementRestoration(WindowId windowId, Guid guid)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesAppWindowEnablePlacementRestoration(windowId, guid));
    }

    public static void SwitchToWindow(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSwitchToWindow(hWnd));
    }

    public static void AddExtendedStyleLayered(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesAddExtendedStyleLayered(hWnd));
    }

    public static void RemoveExtendedStyleLayered(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesRemoveExtendedStyleLayered(hWnd));
    }

    public static void SetLayeredWindowTransparency(HWND hWnd, byte opacity)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetLayeredWindowTransparency(hWnd, opacity));
    }

    public static void AddExtendedStyleToolWindow(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesAddExtendedStyleToolWindow(hWnd));
    }

    public static void RemoveStyleOverlappedWindow(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesRemoveStyleOverlappedWindow(hWnd));
    }

    public static float GetRasterizationScaleForWindow(HWND hWnd)
    {
        float scale;
        Marshal.ThrowExceptionForHR(WindowUtilitiesGetRasterizationScaleForWindow(hWnd, &scale));
        return scale;
    }

    public static void SetWindowIsEnabled(HWND hWnd, BOOL isEnabled)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetWindowIsEnabled(hWnd, isEnabled));
    }

    public static void SetWindowOwner(HWND hWnd, HWND hWndOwner)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetWindowOwner(hWnd, hWndOwner));
    }

    public static void SetTaskbarProgress(HWND hWnd, TBPFLAG state, ulong value, ulong maximum, ref ObjectReference<IUnknownVftbl>? taskbar)
    {
        nint pv = MarshalInterfaceHelper<object>.GetAbi(taskbar);
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetTaskbarProgress(hWnd, state, value, maximum, (IUnknownVftbl**)&pv));
        taskbar = pv == 0 ? null : ObjectReference<IUnknownVftbl>.Attach(ref pv, new("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEA84"));
    }

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesAppWindowEnablePlacementRestoration(WindowId windowId, Guid guid);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesSwitchToWindow(HWND hWnd);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesAddExtendedStyleLayered(HWND hWnd);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesRemoveExtendedStyleLayered(HWND hWnd);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesSetLayeredWindowTransparency(HWND hWnd, byte opacity);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesAddExtendedStyleToolWindow(HWND hWnd);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesRemoveStyleOverlappedWindow(HWND hWnd);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesGetRasterizationScaleForWindow(HWND hWnd, float* scale);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesSetWindowIsEnabled(HWND hWnd, BOOL isEnabled);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesSetWindowOwner(HWND hWnd, HWND hWndOwner);

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT WindowUtilitiesSetTaskbarProgress(HWND hWnd, TBPFLAG state, ulong value, ulong maximum, IUnknownVftbl** ppTaskbar);
}
