using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Nicole.Core.Hosting;
using Snap.Nicole.Native;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.ViewModels.NotifyIcon;
using System;
using System.IO;
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
        this.serviceProvider = serviceProvider;

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.ico");
        Guid id = MemoryMarshal.AsRef<Guid>(CryptographicOperations.HashData(HashAlgorithmName.MD5, Encoding.UTF8.GetBytes(iconPath)));
        native = NicoleNative.Default.MakeNotifyIcon(iconPath, in id);

        hostWindowLifeTime = serviceProvider.GetRequiredService<IWindowLifeTime<NotifyIconXamlHostWindow>>();
        hostWindowLifeTime.Show();
        hostWindowLifeTime.Window.AppWindow.MoveAndResize(default);

        handle = new(this);
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
        native.Create(NicoleNativeNotifyIcon.Callback.Create(&OnNotifyIconCallback), handle, "Snap Nicole");
    }

    public void Recreate()
    {
        if (disposed)
        {
            return;
        }

        native.Recreate("Snap Nicole");
    }

    public void RequestContextMenu(RECT iconRect, POINT cursorPos)
    {
        if (disposed)
        {
            return;
        }

        hostWindowLifeTime.Show();
        flyout ??= new NotifyIconContextMenuFlyout(serviceProvider.GetRequiredService<NotifyIconContextMenuFlyoutViewModel>());
        hostWindowLifeTime.Window.ShowFlyoutAt(flyout, iconRect, cursorPos);
    }

    public void RequestMainWindow()
    {
        if (disposed)
        {
            return;
        }

        serviceProvider.GetRequiredService<IWindowLifeTime<MainWindow>>().Show();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnNotifyIconCallback(NicoleNativeNotifyIcon.CallbackKind kind, RECT iconRect, POINT cursorPos, GCHandle<INotifyIcon> userData)
    {
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
