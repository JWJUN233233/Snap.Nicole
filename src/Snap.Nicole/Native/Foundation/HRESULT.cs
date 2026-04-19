using System.Diagnostics;

namespace Snap.Nicole.Native.Foundation;

[DebuggerDisplay("{DebuggerDisplay}")]
internal readonly partial struct HRESULT
{
    public const int E_ASYNC_OPERATION_NOT_STARTED = unchecked((int)0x80000019);
    public const int E_FAIL = unchecked((int)0x80004005);
    public const int E_NOINTERFACE = unchecked((int)0x80004002);
    public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

    public readonly int Value;

    private string DebuggerDisplay { get => ToString(); }

    public static unsafe implicit operator int(HRESULT value)
    {
        return *(int*)&value;
    }

    public static unsafe implicit operator HRESULT(int value)
    {
        return *(HRESULT*)&value;
    }

    public override string ToString()
    {
        return $"0x{Value:X8}";
    }
}
