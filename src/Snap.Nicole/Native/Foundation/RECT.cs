using Windows.Graphics;

namespace Snap.Nicole.Native.Foundation;

internal struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;

    public RECT(int left, int top, int right, int bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }

    public static implicit operator RectInt32(RECT rect)
    {
        return new RectInt32(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }
}
