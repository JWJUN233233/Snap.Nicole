namespace Snap.Nicole.Core;

internal interface IIdentifiable<T>
    where T : IEquatable<T>
{
    T Id { get; }
}
