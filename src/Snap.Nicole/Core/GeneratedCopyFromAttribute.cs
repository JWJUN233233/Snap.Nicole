using System;

namespace Snap.Nicole.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
internal sealed class GeneratedCopyFromAttribute : Attribute;
