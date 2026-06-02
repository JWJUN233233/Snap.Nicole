using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.IO;
using Snap.Nicole.UI;
using Snap.Nicole.UI.Xaml;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.UI;

namespace Snap.Nicole.Core.Hosting;

internal sealed class WindowLifeTime<TWindow>(IServiceProvider serviceProvider) : IWindowLifeTime<TWindow>
    where TWindow : Window
{
    private static readonly string WindowTypeFullName = TypeNameHelper.GetTypeDisplayName(typeof(TWindow));

    private WindowSubclassLifeTime? subclass;

    public TWindow? Window { get; private set; }

    public void Show()
    {
        string windowTypeName = TypeNameHelper.GetTypeDisplayName(typeof(TWindow), fullName: false);

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.UIWindowShow, WindowTypeFullName);
        span.SetTag(SentryTags.UIWindow, windowTypeName);

        try
        {
            if (Window == null)
            {
                TWindow window = serviceProvider.GetRequiredService<TWindow>();
                window.EnablePlacementRestoration(MemoryMarshal.AsRef<Guid>(CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(WindowTypeFullName))));
                Window = window;

                subclass = new(window);

                AppWindow appWindow = window.AppWindow;

                appWindow.Title = window.Title;
                appWindow.SetIcon(WellKnownLocations.AppIcon);

                if (window is IXamlWindowExtendsContentIntoTitleBar xamlWindow)
                {
                    window.ExtendsContentIntoTitleBar = true;
                    window.SetTitleBar(xamlWindow.TitleBar);

                    AppWindowTitleBar appWindowTitleBar = appWindow.TitleBar;
                    appWindowTitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                    appWindowTitleBar.ExtendsContentIntoTitleBar = true;

                    UpdateTitleButtonColor(default!, default!);
                    xamlWindow.TitleBar.ActualThemeChanged += UpdateTitleButtonColor;
                }

                window.Closed += OnWindowClose;
            }

            Window.Activate();
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.UIWindowShow);
            throw;
        }
    }

    public void Close()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.UIWindowClose, WindowTypeFullName);
        span.SetTag(SentryTags.UIWindow, TypeNameHelper.GetTypeDisplayName(typeof(TWindow), fullName: false));

        try
        {
            Window?.Close();
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.UIWindowClose);
            throw;
        }
    }

    private void OnWindowClose(object ignore, WindowEventArgs args)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.UIWindowClosed, WindowTypeFullName);
        span.SetTag(SentryTags.UIWindow, TypeNameHelper.GetTypeDisplayName(typeof(TWindow), fullName: false));

        if (Window is not { } window)
        {
            return;
        }

        IXamlWindowCloseHandler? handler = window as IXamlWindowCloseHandler;
        if (handler is not null)
        {
            handler.OnWindowClosing(out bool cancel);
            if (cancel)
            {
                args.Handled = true;
                span.SetTag(SentryTags.UIWindowCloseCancelled, true);
                span.Finish(SpanStatus.Cancelled);
                return;
            }
        }

        window.Closed -= OnWindowClose;

        if (window is IXamlWindowExtendsContentIntoTitleBar xamlWindow)
        {
            xamlWindow.TitleBar.ActualThemeChanged -= UpdateTitleButtonColor;
        }

        handler?.OnWindowClosed();

        // 1. Free subclass lifetime
        subclass?.Dispose();
        subclass = null;

        // 2. Clear window reference
        Window = null;
    }

    private void UpdateTitleButtonColor(FrameworkElement discardElement, object e)
    {
        if (Window is not IXamlWindowExtendsContentIntoTitleBar xamlWindow)
        {
            return;
        }

        AppWindowTitleBar appTitleBar = Window.AppWindow.TitleBar;

        appTitleBar.ButtonBackgroundColor = Colors.Transparent;
        appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        bool isDarkMode = ThemeHelper.IsDark(xamlWindow.TitleBar.ActualTheme);

        Color systemBaseLowColor = SystemColors.BaseLowColor(isDarkMode);
        appTitleBar.ButtonHoverBackgroundColor = systemBaseLowColor;

        Color systemBaseMediumLowColor = SystemColors.BaseMediumLowColor(isDarkMode);
        appTitleBar.ButtonPressedBackgroundColor = systemBaseMediumLowColor;

        // The Foreground doesn't accept Alpha channel. So we translate it to gray.
        byte light = (byte)((systemBaseMediumLowColor.R + systemBaseMediumLowColor.G + systemBaseMediumLowColor.B) / 3);
        byte result = (byte)(systemBaseMediumLowColor.A / 255.0 * light);
        appTitleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, result, result, result);

        Color systemBaseHighColor = SystemColors.BaseHighColor(isDarkMode);
        appTitleBar.ButtonForegroundColor = systemBaseHighColor;
        appTitleBar.ButtonHoverForegroundColor = systemBaseHighColor;
        appTitleBar.ButtonPressedForegroundColor = systemBaseHighColor;
    }
}
