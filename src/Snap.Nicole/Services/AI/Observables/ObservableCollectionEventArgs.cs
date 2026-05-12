using System.Collections.Specialized;
using System.ComponentModel;

namespace Snap.Nicole.Services.AI.Observables;

internal static class ObservableCollectionEventArgs
{
    internal static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
    internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
    internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
}
