using Snap.Nicole.Core.Hosting;
using Snap.Nicole.Native.Foundation;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal unsafe sealed class NicoleNative(ObjectReference<NicoleNative.Vftbl> objRef)
{
    public const string DllName = "Snap.Nicole.Native.dll";
    public const string IID_INicoleNative = "10E6C85E-438D-4871-B1E3-2527BF5DC936";
    public const string IID_INicoleNativeFirmwareUuidReader = "84C232A9-E16B-4C70-BFE0-D1CA056E4FAE";
    public const string IID_INicoleNativeNotifyIcon = "6F37022E-238B-426D-8C91-07A431B00FAC";
    public const string IID_INicoleNativeWindowSubclass = "2F14C477-E0CF-40D5-BBA4-7DA17D63C736";

    public static NicoleNative Default { get => LazyInitializer.EnsureInitialized(ref field, NicoleCreateInstance); }

    private static NicoleNative NicoleCreateInstance()
    {
        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, EntryPoint = "NicoleCreateInstance", ExactSpelling = true)]
        static extern HRESULT NativeMethod(Vftbl** ppv);

        nint pv = default;
        Marshal.ThrowExceptionForHR(NativeMethod((Vftbl**)&pv));
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

    public NicoleNativeFirmwareUuidReader MakeFirmwareUuidReader()
    {
        nint pv = default;
        Marshal.ThrowExceptionForHR(objRef.Vftbl.MakeFirmwareUuidReader(objRef.ThisPtr, (NicoleNativeFirmwareUuidReader.Vftbl**)&pv));
        return new(ObjectReference<NicoleNativeFirmwareUuidReader.Vftbl>.Attach(ref pv, typeof(NicoleNativeFirmwareUuidReader.Vftbl).GUID));
    }

    [Guid(IID_INicoleNative)]
    internal readonly struct Vftbl
    {
        internal readonly IUnknownVftbl IUnknownVftbl;
        internal readonly delegate* unmanaged[Stdcall]<nint, PCWSTR, Guid*, NicoleNativeNotifyIcon.Vftbl**, HRESULT> MakeNotifyIcon;
        internal readonly delegate* unmanaged[Stdcall]<nint, HWND, NicoleNativeWindowSubclass.Callback, GCHandle<WindowSubclassLifeTime>, NicoleNativeWindowSubclass.Vftbl**, HRESULT> MakeWindowSubclass;
        internal readonly delegate* unmanaged[Stdcall]<nint, NicoleNativeFirmwareUuidReader.Vftbl**, HRESULT> MakeFirmwareUuidReader;
    }
}
