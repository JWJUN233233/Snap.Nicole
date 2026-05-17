namespace Snap.Nicole.ViewModels.Settings;

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
