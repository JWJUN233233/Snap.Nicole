using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionResultContent : ObservableToolResultContent
{
    [ObservableProperty]
    public partial object? Result { get; set; }

    [ObservableProperty]
    public partial Exception? Exception { get; set; }

    public static ObservableFunctionResultContent Create(FunctionResultContent functionResultContent)
    {
        return new()
        {
            CallId = functionResultContent.CallId,
            Result = functionResultContent.Result,
            Exception = functionResultContent.Exception,
            RawRepresentation = functionResultContent.RawRepresentation,
        };
    }
}
