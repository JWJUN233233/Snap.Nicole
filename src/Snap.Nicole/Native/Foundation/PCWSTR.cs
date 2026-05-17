namespace Snap.Nicole.Native.Foundation;

internal readonly partial struct PCWSTR
{
    public readonly unsafe char* Value;

    public static unsafe implicit operator PCWSTR(char* value)
    {
        return *(PCWSTR*)&value;
    }

    public static unsafe implicit operator char*(PCWSTR value)
    {
        return *(char**)&value;
    }
}
