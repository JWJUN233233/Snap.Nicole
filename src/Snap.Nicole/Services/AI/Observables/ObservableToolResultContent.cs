using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal partial class ObservableToolResultContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string CallId { get; set; }
}
