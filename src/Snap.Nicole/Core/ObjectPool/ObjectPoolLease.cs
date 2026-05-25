using Microsoft.Extensions.ObjectPool;

namespace Snap.Nicole.Core.ObjectPool;

public ref struct ObjectPoolLease<T>(ObjectPool<T> objectPool) : IDisposable
    where T : class
{
    private readonly ObjectPool<T> objectPool = objectPool;
    private T value = objectPool.Get();

    public T Value { get => value ?? throw new ObjectDisposedException(nameof(ObjectPoolLease<>)); }

    public void Dispose()
    {
        T value = this.value;
        if (value is null)
        {
            return;
        }

        this.value = null!;
        objectPool.Return(value);
    }
}
