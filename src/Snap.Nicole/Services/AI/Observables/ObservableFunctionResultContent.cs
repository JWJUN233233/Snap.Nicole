using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionResultContent : ObservableToolResultContent
{
    [ObservableProperty]
    public partial string? Result { get; set; }

    [ObservableProperty]
    [JsonIgnore]
    public partial Exception? Exception { get; set; }

    public static ObservableFunctionResultContent Create(FunctionResultContent functionResultContent, JsonSerializerOptions jsonOptions)
    {
        return new()
        {
            CallId = functionResultContent.CallId,
            Result = SerializeResult(functionResultContent.Result, jsonOptions),
            Exception = functionResultContent.Exception,
        };
    }

    private static string? SerializeResult(object? value, JsonSerializerOptions jsonOptions)
    {
        if (value is string stringValue)
        {
            return stringValue;
        }

        if (value is JsonElement { ValueKind: JsonValueKind.String } stringElement)
        {
            return stringElement.GetString();
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
