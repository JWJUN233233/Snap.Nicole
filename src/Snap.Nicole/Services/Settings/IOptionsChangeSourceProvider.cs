using System.Collections.Generic;
using System.ComponentModel;

namespace Snap.Nicole.Services.Settings;

internal interface IOptionsChangeSourceProvider
{
    IEnumerable<INotifyPropertyChanged> GetChangeSources();
}
