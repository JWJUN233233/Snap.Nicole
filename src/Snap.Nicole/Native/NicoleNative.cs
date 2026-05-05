using Snap.Nicole.Core.Hosting;
using Snap.Nicole.Native.Foundation;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal unsafe sealed class NicoleNative(ObjectReference<NicoleNative.Vftbl> objRef)
{
    public const string DllName = "Snap.Nicole.Native.dll";
    public const string IID_INicoleNative = "E5EEEB3A-C782-4C90-8F93-91830D7F1F58";
    public const string IID_INicoleNativeNotifyIcon = "6F37022E-238B-426D-8C91-07A431B00FAC";
    public const string IID_INicoleNativeWindowSubclass = "2F14C477-E0CF-40D5-BBA4-7DA17D63C736";

    public static NicoleNative Default { get => LazyInitializer.EnsureInitialized(ref field, NicoleCreateInstance); }

    private static NicoleNative NicoleCreateInstance()
    {
        nint pv = default;
        Marshal.ThrowExceptionForHR(NicoleCreateInstance((Vftbl**)&pv));
        return new NicoleNative(ObjectReference<Vftbl>.Attach(ref pv, typeof(Vftbl).GUID));
    }

    public NicoleNativeNotifyIcon MakeNotifyIcon(ReadOnlySpan<char> iconPath, ref readonly Guid id)
    {
        fixed (char* pIconPath = iconPath)
        {
            fixed (Guid* pId = &id)
            {
                nint pv = default;
                Marshal.ThrowExceptionForHR(objRef.Vftbl.MakeNotifyIcon(objRef.ThisPtr, pIconPath, pId, (NicoleNativeNotifyIcon.Vftbl**)&pv));
                return new(ObjectReference<NicoleNativeNotifyIcon.Vftbl>.Attach(ref pv, typeof(NicoleNativeNotifyIcon.Vftbl).GUID));
            }
        }
    }

    public NicoleNativeWindowSubclass MakeWindowSubclass(HWND hWnd, NicoleNativeWindowSubclass.Callback callback, GCHandle<WindowSubclassLifeTime> userData)
    {
        nint pv = default;
        Marshal.ThrowExceptionForHR(objRef.Vftbl.MakeWindowSubclass(objRef.ThisPtr, hWnd, callback, userData, (NicoleNativeWindowSubclass.Vftbl**)&pv));
        return new(ObjectReference<NicoleNativeWindowSubclass.Vftbl>.Attach(ref pv, typeof(NicoleNativeWindowSubclass.Vftbl).GUID));
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT NicoleCreateInstance(Vftbl** ppv);

    [Guid(IID_INicoleNative)]
    internal readonly struct Vftbl
    {
#pragma warning disable CS0649
        internal readonly IUnknownVftbl IUnknownVftbl;
        internal readonly delegate* unmanaged[Stdcall]<nint, PCWSTR, Guid*, NicoleNativeNotifyIcon.Vftbl**, HRESULT> MakeNotifyIcon;
        internal readonly delegate* unmanaged[Stdcall]<nint, HWND, NicoleNativeWindowSubclass.Callback, GCHandle<WindowSubclassLifeTime>, NicoleNativeWindowSubclass.Vftbl**, HRESULT> MakeWindowSubclass;
#pragma warning restore CS0649
    }
}
