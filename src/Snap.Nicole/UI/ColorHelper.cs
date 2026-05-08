using System.Buffers.Binary;
using Windows.UI;

namespace Snap.Nicole.UI;

internal static class ColorHelper
{
    public static unsafe Color ToColor(uint value)
    {
        uint reversed = BinaryPrimitives.ReverseEndianness(value);
        return *(Color*)&reversed;
    }
}
