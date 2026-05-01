using Microsoft.UI.Xaml.Controls;
using System;

namespace Snap.Nicole.UI.Xaml.Navigation;

internal interface INavigationService
{
    Frame? Frame { get; set; }

    NavigationView? NavigationView { get; set; }

    bool NavigateTo(Type pageType);

    bool NavigateTo(NavigationViewItem item);
}
