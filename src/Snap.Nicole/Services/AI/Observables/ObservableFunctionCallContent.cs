using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableFunctionCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial IDictionary<string, object?>? Arguments { get; set; }

    [ObservableProperty]
    public partial Exception? Exception { get; set; }

    [ObservableProperty]
    public partial bool InformationalOnly { get; set; }
}
