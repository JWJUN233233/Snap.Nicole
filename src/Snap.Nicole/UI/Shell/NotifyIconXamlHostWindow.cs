using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Core.Hosting;
using Snap.Nicole.UI.Xaml;
using System;

namespace Snap.Nicole.UI.Shell;

internal sealed class NotifyIconXamlHostWindow : Window, IXamlWindowEraseBackground, IXamlWindowCloseHandler
{
    private readonly IServiceProvider serviceProvider;

    public NotifyIconXamlHostWindow(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
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

    public void OnWindowClosing(out bool cancel)
    {
        cancel = !serviceProvider.GetRequiredService<IApplicationLifeTime>().IsExiting;
    }
}
