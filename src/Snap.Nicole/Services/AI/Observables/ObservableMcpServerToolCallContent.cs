using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableMcpServerToolCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? ServerName { get; set; }

    [ObservableProperty]
    public partial IDictionary<string, object?>? Arguments { get; set; }
}
