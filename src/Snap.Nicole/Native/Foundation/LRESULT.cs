namespace Snap.Nicole.Native.Foundation;

internal readonly struct LRESULT
{
    public readonly nint Value;

    public static unsafe implicit operator LRESULT(nint value)
    {
        return *(LRESULT*)&value;
    }

    public static unsafe implicit operator LRESULT(BOOL value)
    {
        return *(int*)&value;
    }
}