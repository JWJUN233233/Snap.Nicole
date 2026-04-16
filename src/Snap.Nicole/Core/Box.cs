using System;
using System.Collections.Generic;
using System.Text;

namespace Snap.Nicole.Core;

internal sealed class Box<T>
{
    public T? Value { get; set; }
}