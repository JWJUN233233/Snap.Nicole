using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionCallContent : ObservableToolCallContent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? Arguments { get; set; }

    [ObservableProperty]
    public partial Exception? Exception { get; set; }

    [ObservableProperty]
    public partial bool InformationalOnly { get; set; }

    public static ObservableFunctionCallContent Create(FunctionCallContent functionCallContent)
    {
        return new()
        {
            CallId = functionCallContent.CallId,
            Name = functionCallContent.Name,
            Arguments = SerializeArguments(functionCallContent.Arguments),
            RawRepresentation = functionCallContent.RawRepresentation,
        };
    }

    private static string? SerializeArguments(IDictionary<string, object?>? value)
    {
        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch
        {
            return value?.ToString();
        }
    }
}
