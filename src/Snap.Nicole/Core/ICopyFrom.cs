namespace Snap.Nicole.Core;

public interface ICopyFrom<T>
{
    void CopyFrom(T source);
}