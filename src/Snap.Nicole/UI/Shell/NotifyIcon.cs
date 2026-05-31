using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Nicole.Core.Hosting;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.IO;
using Snap.Nicole.Native;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.UI.Xaml.Windows;
using Snap.Nicole.ViewModels.NotifyIcon;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Snap.Nicole.UI.Shell;

internal sealed class NotifyIcon : INotifyIcon, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly IWindowLifeTime<NotifyIconXamlHostWindow> hostWindowLifeTime;
    private readonly NicoleNativeNotifyIcon native;
    private FlyoutBase? flyout;

    private GCHandle<INotifyIcon> handle;
    private bool disposed;

    public NotifyIcon(IServiceProvider serviceProvider)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ui.notify_icon.initialize", "Initialize notify icon");

        try
        {
            this.serviceProvider = serviceProvider;

            string iconPath = WellKnownLocations.AppIcon;
            Guid id = MemoryMarshal.AsRef<Guid>(CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(iconPath)));
            native = NicoleNative.Default.MakeNotifyIcon(iconPath, in id);

            hostWindowLifeTime = serviceProvider.GetRequiredService<IWindowLifeTime<NotifyIconXamlHostWindow>>();
            hostWindowLifeTime.Show();
            hostWindowLifeTime.Window.AppWindow.MoveAndResize(default);

            handle = new(this);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ui.notify_icon.initialize");
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        native.Destroy();
        handle.Dispose();

        flyout = null;
    }

    public unsafe void Create()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ui.notify_icon.create", "Create notify icon");

        try
        {
            native.Create(NicoleNativeNotifyIcon.Callback.Create(&OnNotifyIconCallback), handle, "Snap Nicole");
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ui.notify_icon.create");
            throw;
        }
    }

    public void Recreate()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ui.notify_icon.recreate", "Recreate notify icon");

        if (disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        try
        {
            native.Recreate("Snap Nicole");
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ui.notify_icon.recreate");
            throw;
        }
    }

    public void RequestContextMenu(RECT iconRect, POINT cursorPos)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ui.notify_icon.context_menu", "Open notify icon context menu");

        if (disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        try
        {
            hostWindowLifeTime.Show();
            flyout ??= new NotifyIconContextMenuFlyout(serviceProvider.GetRequiredService<NotifyIconContextMenuFlyoutViewModel>());
            hostWindowLifeTime.Window.ShowFlyoutAt(flyout, iconRect, cursorPos);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ui.notify_icon.context_menu");
            throw;
        }
    }

    public void RequestMainWindow()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("ui.notify_icon.main_window", "Open main window from notify icon");

        if (disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        try
        {
            serviceProvider.GetRequiredService<IWindowLifeTime<MainWindow>>().Show();
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, "ui.notify_icon.main_window");
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnNotifyIconCallback(NicoleNativeNotifyIcon.CallbackKind kind, RECT iconRect, POINT cursorPos, GCHandle<INotifyIcon> userData)
    {
        Dictionary<string, string> data = new()
        {
            ["kind"] = kind.ToString(),
        };
        SentryDiagnostics.AddBreadcrumb("Notify icon callback", "ui.notify_icon", "ui", data);

        if (userData.Target is not { } notifyIcon)
        {
            return;
        }

        switch (kind)
        {
            case NicoleNativeNotifyIcon.CallbackKind.TaskbarCreated:
                notifyIcon.Recreate();
                break;
            case NicoleNativeNotifyIcon.CallbackKind.ContextMenu:
            case NicoleNativeNotifyIcon.CallbackKind.LeftButtonDown:
                notifyIcon.RequestContextMenu(iconRect, cursorPos);
                break;
            case NicoleNativeNotifyIcon.CallbackKind.LeftButtonDoubleClick:
                notifyIcon.RequestMainWindow();
                break;
        }
    }
}
