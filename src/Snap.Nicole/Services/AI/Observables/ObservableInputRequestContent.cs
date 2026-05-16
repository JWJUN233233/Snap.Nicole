using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal partial class ObservableInputRequestContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string RequestId { get; set; }
}
