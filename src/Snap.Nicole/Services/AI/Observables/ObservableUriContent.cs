using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableUriContent : ObservableAIContent
{
    [ObservableProperty]
    public partial Uri Uri { get; set; }

    [ObservableProperty]
    public partial string MediaType { get; set; }
}
