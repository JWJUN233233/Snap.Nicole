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

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT NicoleCreateInstance(Vftbl** ppv);

    [Guid(IID_INicoleNative)]
    internal readonly struct Vftbl
    {
        internal readonly IUnknownVftbl IUnknownVftbl;
        // virtual HRESULT APIENTRY MakeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, INicoleNativeNotifyIcon** ppv) = 0;
        internal readonly delegate* unmanaged[Stdcall]<nint, PCWSTR, Guid*, NicoleNativeNotifyIcon.Vftbl**, HRESULT> MakeNotifyIcon;
    }
}
