using Snap.Nicole.Native.Foundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Nicole.Native;

internal static class XamlUtilities
{
    public static unsafe void PatchFontAndScriptServicesGetDefaultFontNameString(ReadOnlySpan<char> resource)
    {
        fixed (char* pResource = resource)
        {
            Marshal.ThrowExceptionForHR(PatchFontAndScriptServicesGetDefaultFontNameString(pResource));
        }
    }

    [DllImport(NicoleNative.DllName, CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern HRESULT PatchFontAndScriptServicesGetDefaultFontNameString(PCWSTR resource);
}