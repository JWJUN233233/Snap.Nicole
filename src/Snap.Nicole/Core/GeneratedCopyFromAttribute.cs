using System;

namespace Snap.Nicole.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
internal sealed class GeneratedCopyFromAttribute<T> : Attribute;
