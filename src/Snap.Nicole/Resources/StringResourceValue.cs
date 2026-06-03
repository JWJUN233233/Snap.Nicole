using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace Snap.Nicole.Resources;

internal sealed class StringResourceValue : ObservableObject
{
    private static readonly List<WeakReference<StringResourceValue>> registeredValues = [];

    static StringResourceValue()
    {
        StringResourceProxy.Default.PropertyChanged += OnStringResourceChanged;
    }

    private StringResourceValue()
    {
        lock (registeredValues)
        {
            registeredValues.Add(new(this));
        }
    }

    public SRName? Name { get; init; }

    public object?[]? Arguments { get; init; }

    public string Text { get; init; } = string.Empty;

    public string Value
    {
        get
        {
            if (Name is not SRName name)
            {
                return Text;
            }

            string value = StringResourceProxy.Default[name];
            if (Arguments is { Length: > 0 } arguments)
            {
                value = string.Format(StringResourceProxy.Default.CurrentCulture, value, NormalizeArguments(arguments));
            }

            return value;
        }
    }

    public static StringResourceValue FromName(SRName name)
    {
        return new()
        {
            Name = name,
        };
    }

    public static StringResourceValue FromName(SRName name, params object?[]? arguments)
    {
        return new()
        {
            Name = name,
            Arguments = arguments,
        };
    }

    public static StringResourceValue FromText(string? text)
    {
        return new()
        {
            Text = text ?? string.Empty,
        };
    }

    private static void OnStringResourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not "Item[]")
        {
            return;
        }

        List<StringResourceValue> liveValues = [];
        lock (registeredValues)
        {
            for (int i = registeredValues.Count - 1; i >= 0; i--)
            {
                if (registeredValues[i].TryGetTarget(out StringResourceValue? value))
                {
                    liveValues.Add(value);
                }
                else
                {
                    registeredValues.RemoveAt(i);
                }
            }
        }

        foreach (StringResourceValue value in liveValues)
        {
            value.OnPropertyChanged(nameof(Value));
        }
    }

    private static object?[] NormalizeArguments(object?[] arguments)
    {
        object?[] normalized = new object?[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            normalized[i] = NormalizeArgument(arguments[i]);
        }

        return normalized;
    }

    private static object? NormalizeArgument(object? argument)
    {
        return argument switch
        {
            SRName name => StringResourceProxy.Default[name],
            StringResourceValue value => value.Value,
            _ => argument,
        };
    }
}
