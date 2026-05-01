using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Snap.Nicole.UI.Xaml.Markup;

public sealed class NavigationExtensions
{
    public static readonly DependencyProperty NavigateToProperty =
        DependencyProperty.RegisterAttached(
            "NavigateTo",
            typeof(Type),
            typeof(NavigationExtensions),
            new PropertyMetadata(null));

    public static Type? GetNavigateTo(NavigationViewItem obj)
    {
        return (Type?)obj.GetValue(NavigateToProperty);
    }

    public static void SetNavigateTo(NavigationViewItem obj, Type? value)
    {
        obj.SetValue(NavigateToProperty, value);
    }
}
