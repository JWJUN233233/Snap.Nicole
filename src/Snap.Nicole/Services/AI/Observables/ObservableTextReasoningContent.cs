using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableTextReasoningContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string Text { get; set; }
}
