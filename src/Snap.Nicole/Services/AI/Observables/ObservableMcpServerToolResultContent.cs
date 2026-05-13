using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableMcpServerToolResultContent : ObservableToolResultContent
{
    [ObservableProperty]
    public partial ObservableAIContentCollection? Outputs { get; set; }
}
