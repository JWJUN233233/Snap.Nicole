namespace Snap.Nicole.UI.Xaml;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class GeneratedDependencyPropertyAttribute<T>(string name) : Attribute
{
    public bool IsAttached { get; set; }

    public Type? TargetType { get; set; }

    public object? DefaultValue { get; set; }

    public string? CreateDefaultValueCallbackName { get; set; }

    public string? PropertyChangedCallbackName { get; set; }

    public bool NotNull { get; set; }
}
