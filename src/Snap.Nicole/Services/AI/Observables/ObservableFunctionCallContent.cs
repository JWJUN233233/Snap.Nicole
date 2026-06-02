using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? Arguments { get; set; }

    [ObservableProperty]
    [property: JsonIgnore]
    public partial Exception? Exception { get; set; }

    [ObservableProperty]
    public partial bool InformationalOnly { get; set; }

    public static ObservableFunctionCallContent Create(FunctionCallContent functionCallContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            CallId = functionCallContent.CallId,
            Name = functionCallContent.Name,
            Arguments = SerializeArguments(functionCallContent.Arguments, jsonOptions),
        };
    }

    private static string? SerializeArguments(IDictionary<string, object?>? value, JsonSerializerOptions jsonOptions)
    {
        if (value is null or { Count: 0 })
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(value, jsonOptions);
        }
        catch
        {
            return value?.ToString();
        }
    }
}
