using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionResultContent : ObservableToolResultContent
{
    [ObservableProperty]
    public partial object? Result { get; set; }

    [ObservableProperty]
    public partial Exception? Exception { get; set; }
}
