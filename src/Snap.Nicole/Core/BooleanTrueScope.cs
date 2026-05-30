namespace Snap.Nicole.Core;

internal ref struct BooleanTrueScope : IDisposable
{
    private ref bool value;

    public BooleanTrueScope(ref bool value)
    {
        this.value = ref value;
        value = true;
    }

    public static BooleanTrueScope Create(ref bool value)
    {
        return new(ref value);
    }

    public void Dispose()
    {
        value = false;
    }
}
