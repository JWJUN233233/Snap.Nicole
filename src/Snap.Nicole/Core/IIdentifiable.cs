namespace Snap.Nicole.Core;

internal interface IIdentifiable<T>
    where T : struct, IEquatable<T>
{
    T Id { get; }
}
