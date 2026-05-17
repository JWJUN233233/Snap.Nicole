using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Nicole.Core.Hosting;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.UI.Xaml;
using System;
using Windows.Graphics;

namespace Snap.Nicole.UI.Shell;

internal sealed class NotifyIconXamlHostWindow : Window, IXamlWindowEraseBackground, IXamlWindowCloseHandler
{
    private readonly IServiceProvider serviceProvider;

    public NotifyIconXamlHostWindow(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        // The Border class is specially chosen to bring minimal overhead
        Content = new Border();

        // 1. AddExtendedStyleLayered(): Allowing the window to be set to transparent and click-through
        // 2. SetLayeredWindowTransparency(0): Making the window fully transparent
        // 3. AddExtendedStyleToolWindow(): Allowing the window can be set to smaller size than the window control buttons
        this.AddExtendedStyleLayered();
        this.SetLayeredWindowTransparency(0);
        this.AddExtendedStyleToolWindow();

        AppWindow.Title = "SnapNicoleNotifyIconXamlHost";
        AppWindow.IsShownInSwitchers = false;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(false, false);
        }
    }

    public void OnWindowClosing(out bool cancel)
    {
        cancel = !serviceProvider.GetRequiredService<IApplicationLifeTime>().IsExiting;
    }

    public void ShowFlyoutAt(FlyoutBase flyout, RECT icon, POINT point)
    {
        // Enlarge the icon rect to make the flyout position better
        icon.left -= 8;
        icon.top -= 8;
        icon.right += 8;
        icon.bottom += 8;

        if (AppWindow is null || Content?.XamlRoot is null /*ERROR_XAMLROOT_REQUIRED*/)
        {
            return;
        }

        this.SwitchTo();
        AppWindow.MoveAndResize(new(icon.left, icon.top, icon.right - icon.left, icon.bottom - icon.top));

        flyout.ShowAt(Content, new()
        {
            Placement = FlyoutPlacementMode.Auto,
            ShowMode = FlyoutShowMode.Standard,
        });
    }
}
