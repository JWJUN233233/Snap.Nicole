namespace Snap.Nicole.ViewModels;

internal sealed record SettingsOption<T>
{
    public SettingsOption(string label, T value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public T Value { get; }
}