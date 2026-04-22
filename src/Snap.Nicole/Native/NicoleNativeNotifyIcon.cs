using Snap.Nicole.Native.Foundation;
using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.Native;

internal sealed unsafe partial class NicoleNativeNotifyIcon(ObjectReference<NicoleNativeNotifyIcon.Vftbl> objRef)
{
    public BOOL IsPromoted
    {
        get
        {
            BOOL promoted = default;
            Marshal.ThrowExceptionForHR(objRef.Vftbl.IsPromoted(objRef.ThisPtr, &promoted));
            return promoted;
        }
    }

    public void Create(Callback callback, nint userData, ReadOnlySpan<char> tip)
    {
        fixed (char* pTip = tip)
        {
            Marshal.ThrowExceptionForHR(objRef.Vftbl.Create(objRef.ThisPtr, callback, userData, pTip));
        }
    }

    public void Recreate(ReadOnlySpan<char> tip)
    {
        fixed (char* pTip = tip)
        {
            Marshal.ThrowExceptionForHR(objRef.Vftbl.Recreate(objRef.ThisPtr, pTip));
        }
    }

    public void Destroy()
    {
        Marshal.ThrowExceptionForHR(objRef.Vftbl.Destroy(objRef.ThisPtr));
    }

    internal enum CallbackKind
    {
        None = 0,
        TaskbarCreated = 1,
        ContextMenu = 2,
        LeftButtonDown = 3,
        LeftButtonDoubleClick = 4,
    }

    internal readonly unsafe partial struct Callback
    {
        [GeneratedUnmanagedFunctionPointer]
        private readonly delegate* unmanaged[Stdcall]<CallbackKind, RECT, POINT, nint, void> value;
    }

    [Guid(NicoleNative.IID_INicoleNativeNotifyIcon)]
    internal readonly struct Vftbl
    {
#pragma warning disable CS0649
        internal readonly IUnknownVftbl IUnknownVftbl;
        internal readonly delegate* unmanaged[Stdcall]<nint, Callback, nint, PCWSTR, HRESULT> Create;
        internal readonly delegate* unmanaged[Stdcall]<nint, PCWSTR, HRESULT> Recreate;
        internal readonly delegate* unmanaged[Stdcall]<nint, HRESULT> Destroy;
        internal readonly delegate* unmanaged[Stdcall]<nint, BOOL*, HRESULT> IsPromoted;
#pragma warning restore CS0649
    }
}