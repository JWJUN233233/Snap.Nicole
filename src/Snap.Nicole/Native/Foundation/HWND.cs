using System;
using System.Collections.Generic;
using System.Text;

namespace Snap.Nicole.Native.Foundation;

internal readonly struct HWND
{
    public readonly nint Value;

    public static unsafe implicit operator HWND(nint value)
    {
        return *(HWND*)&value;
    }

    public static unsafe implicit operator nint(HWND value)
    {
        return *(nint*)&value;
    }
}
