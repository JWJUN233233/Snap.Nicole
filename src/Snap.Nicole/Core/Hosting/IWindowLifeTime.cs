using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;

namespace Snap.Nicole.Core.Hosting;

internal interface IWindowLifeTime<TWindow>
    where TWindow : Window
{
    TWindow? Window { get; }

    [MemberNotNull(nameof(Window))]
    void Show();

    void Close();
}