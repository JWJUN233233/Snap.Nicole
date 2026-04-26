namespace Snap.Nicole.Native.Foundation;

internal readonly struct WPARAM
{
    public readonly nuint Value;

    public WPARAM(nuint value)
    {
        Value = value;
    }

    public static unsafe implicit operator uint(WPARAM value)
    {
        return (uint)*(nuint*)&value;
    }

    public static unsafe implicit operator WPARAM(uint value)
    {
        nuint data = value;
        return *(WPARAM*)&data;
    }

    public static implicit operator WPARAM(ushort value)
    {
        return new(value);
    }
}
