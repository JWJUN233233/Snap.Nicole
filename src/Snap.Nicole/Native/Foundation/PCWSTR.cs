using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Snap.Nicole.Native.Foundation;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal readonly partial struct PCWSTR
{
    public readonly unsafe char* Value;

    public unsafe string DebuggerDisplay { get => MemoryMarshal.CreateReadOnlySpanFromNullTerminated(Value).ToString(); }

    public static unsafe implicit operator PCWSTR(char* value)
    {
        return *(PCWSTR*)&value;
    }

    public static unsafe implicit operator char*(PCWSTR value)
    {
        return *(char**)&value;
    }
}
