using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableChatMessageCollection : ObservableCollection<ObservableChatMessage>
{
    public ObservableChatMessageCollection()
        : base()
    {
    }

    public ObservableChatMessageCollection(IEnumerable<ObservableChatMessage> collection)
        : base(collection)
    {
    }

    public ObservableChatMessageCollection(List<ObservableChatMessage> list)
        : base(list)
    {
    }
}
