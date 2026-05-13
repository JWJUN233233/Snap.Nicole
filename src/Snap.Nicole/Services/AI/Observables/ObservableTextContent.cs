using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableTextContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string Text { get; set; }
}
