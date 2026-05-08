namespace Snap.Nicole.ViewModels;

internal sealed record SettingsItem<T>
{
    public SettingsItem(string label, T value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public T Value { get; }
}