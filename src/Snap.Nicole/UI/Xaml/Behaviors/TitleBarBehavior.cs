using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.UI.Xaml.Navigation;

namespace Snap.Nicole.UI.Xaml.Behaviors;

internal sealed partial class TitleBarBehavior : BehaviorBase<TitleBar>
{
    protected override bool Initialize()
    {
        if (!base.Initialize())
        {
            return false;
        }

        AssociatedObject.BackRequested += OnBackRequested;
        AssociatedObject.PaneToggleRequested += OnPaneToggleRequested;
        return true;
    }

    protected override bool Uninitialize()
    {
        AssociatedObject.BackRequested -= OnBackRequested;
        AssociatedObject.PaneToggleRequested -= OnPaneToggleRequested;
        return base.Uninitialize();
    }

    private static void OnBackRequested(TitleBar sender, object args)
    {
        App.Host.Services.GetRequiredService<IMessenger>().Send(new NavigationGoBackMessage());
    }

    private static void OnPaneToggleRequested(TitleBar sender, object args)
    {
        App.Host.Services.GetRequiredService<IMessenger>().Send(new NavigationPaneToggleMessage());
    }
}
