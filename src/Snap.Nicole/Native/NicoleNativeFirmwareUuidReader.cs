using Snap.Nicole.Native.Foundation;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal sealed unsafe class NicoleNativeFirmwareUuidReader(ObjectReference<NicoleNativeFirmwareUuidReader.Vftbl> objRef)
{
    public bool TryGetFirmwareUuid(out Guid firmwareUuid)
    {
        firmwareUuid = default;

        fixed (Guid* pFirmwareUuid = &firmwareUuid)
        {
            HRESULT result = objRef.Vftbl.GetFirmwareUuid(objRef.ThisPtr, pFirmwareUuid);
            if (result < 0)
            {
                firmwareUuid = default;
                return false;
            }
        }

        return firmwareUuid != Guid.Empty;
    }

    [Guid(NicoleNative.IID_INicoleNativeFirmwareUuidReader)]
    internal readonly struct Vftbl
    {
        internal readonly IUnknownVftbl IUnknownVftbl;
        internal readonly delegate* unmanaged[Stdcall]<nint, Guid*, HRESULT> GetFirmwareUuid;
    }
}
