using Snap.Nicole.Native.Foundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Nicole.Native;

internal static class XamlUtilities
{
    public static unsafe void PatchFontAndScriptServicesGetDefaultFontNameString(ReadOnlySpan<char> resource)
    {
        [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        static extern HRESULT NativeMethod(PCWSTR resource);

        fixed (char* pResource = resource)
        {
            Marshal.ThrowExceptionForHR(NativeMethod(pResource));
        }
    }
}