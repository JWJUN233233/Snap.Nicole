using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed partial class ObservableWebSearchToolCallContent : ObservableToolCallContent
{
    [ObservableProperty]
    public partial ObservableCollection<string>? Queries { get; set; }
}
