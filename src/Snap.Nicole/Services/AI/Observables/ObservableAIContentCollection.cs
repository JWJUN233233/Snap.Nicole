using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableAIContentCollection : ObservableCollection<ObservableAIContent>
{
    public ObservableAIContentCollection()
        : base()
    {
    }

    public ObservableAIContentCollection(IEnumerable<ObservableAIContent> collection)
        : base(collection)
    {
    }

    public ObservableAIContentCollection(List<ObservableAIContent> list)
        : base(list)
    {
    }
}