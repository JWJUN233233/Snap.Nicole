using Snap.Nicole.Native.Foundation;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal sealed unsafe class NicoleNativeFirmwareUuidReader(ObjectReference<NicoleNativeFirmwareUuidReader.Vftbl> objRef)
{
    public Guid FirmwareUuid
    {
        get
        {
            Guid firmwareUuid = default;
            Marshal.ThrowExceptionForHR(objRef.Vftbl.GetFirmwareUuid(objRef.ThisPtr, &firmwareUuid));
            return firmwareUuid;
        }
    }

    [Guid(NicoleNative.IID_INicoleNativeFirmwareUuidReader)]
    internal readonly struct Vftbl
    {
        internal readonly IUnknownVftbl IUnknownVftbl;
        internal readonly delegate* unmanaged[Stdcall]<nint, Guid*, HRESULT> GetFirmwareUuid;
    }
}
