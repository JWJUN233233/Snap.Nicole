using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.UI.Xaml;
using System;

namespace Snap.Nicole.UI.Shell;

internal sealed class NotifyIconXamlHostWindow : Window, IXamlWindowEraseBackground
{
    public NotifyIconXamlHostWindow(IServiceProvider serviceProvider)
    {
        Content = new Border();

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
}
