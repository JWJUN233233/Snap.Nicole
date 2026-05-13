using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableUsageContent : ObservableAIContent
{
    [ObservableProperty]
    public partial UsageDetails Details { get; set; }
}
