using Microsoft.UI.Xaml;

namespace Snap.Nicole.Core.Hosting;

internal interface IWindowSubclassLifeTime
{
    Window Window { get; }
}