using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionResultContent : ObservableToolResultContent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [ObservableProperty]
    public partial string? Result { get; set; }

    [ObservableProperty]
    public partial Exception? Exception { get; set; }

    public static ObservableFunctionResultContent Create(FunctionResultContent functionResultContent)
    {
        return new()
        {
            CallId = functionResultContent.CallId,
            Result = SerializeResult(functionResultContent.Result),
            Exception = functionResultContent.Exception,
        };
    }

    private static string? SerializeResult(object? value)
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
