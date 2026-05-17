using Snap.Nicole.Core.Hosting;
using Snap.Nicole.Native.Foundation;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal sealed unsafe partial class NicoleNativeWindowSubclass(ObjectReference<NicoleNativeWindowSubclass.Vftbl> objRef)
{
    public void Attach()
    {
        Marshal.ThrowExceptionForHR(objRef.Vftbl.Attach(objRef.ThisPtr));
    }

    public void Detach()
    {
        Marshal.ThrowExceptionForHR(objRef.Vftbl.Detach(objRef.ThisPtr));
    }

    internal readonly unsafe partial struct Callback
    {
        [GeneratedUnmanagedFunctionPointer]
        private readonly delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, GCHandle<WindowSubclassLifeTime>, LRESULT*, BOOL> value;
    }

    [Guid(NicoleNative.IID_INicoleNativeWindowSubclass)]
    internal readonly struct Vftbl
    {
        internal readonly IUnknownVftbl IUnknownVftbl;
        internal readonly delegate* unmanaged[Stdcall]<nint, HRESULT> Attach;
        internal readonly delegate* unmanaged[Stdcall]<nint, HRESULT> Detach;
    }
}
