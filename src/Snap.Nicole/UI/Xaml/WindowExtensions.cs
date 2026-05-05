using Microsoft.UI.Xaml;
using Snap.Nicole.Native;
using Snap.Nicole.Native.Foundation;
using Snap.Nicole.Native.UI.Shell;
using System;
using WinRT;
using WinRT.Interop;

namespace Snap.Nicole.UI.Xaml;

internal static class WindowExtensions
{
    extension(Window window)
    {
        public double RasterizationScale
        {
            get
            {
                return window is { Content.XamlRoot: { } xamlRoot }
                    ? xamlRoot.RasterizationScale
                    : WindowUtilities.GetRasterizationScaleForWindow(window.WindowHandle);
            }
        }

        public HWND WindowHandle
        {
            get => WindowNative.GetWindowHandle(window);
        }

        public void EnablePlacementRestoration(Guid guid)
        {
            WindowUtilities.AppWindowEnablePlacementRestoration(window.AppWindow.Id, guid);
        }

        public void SwitchTo()
        {
            WindowUtilities.SwitchToWindow(window.WindowHandle);
        }

        public void AddExtendedStyleLayered()
        {
            WindowUtilities.AddExtendedStyleLayered(window.WindowHandle);
        }

        public void RemoveExtendedStyleLayered()
        {
            WindowUtilities.RemoveExtendedStyleLayered(window.WindowHandle);
        }

        public void SetLayeredWindowTransparency(byte alpha)
        {
            WindowUtilities.SetLayeredWindowTransparency(window.WindowHandle, alpha);
        }

        public void AddExtendedStyleToolWindow()
        {
            WindowUtilities.AddExtendedStyleToolWindow(window.WindowHandle);
        }

        public void RemoveStyleOverlappedWindow()
        {
            WindowUtilities.RemoveStyleOverlappedWindow(window.WindowHandle);
        }

        public void SetTaskbarProgress(TBPFLAG state, ulong value, ulong maximum, ref ObjectReference<IUnknownVftbl>? taskbar)
        {
            WindowUtilities.SetTaskbarProgress(window.WindowHandle, state, value, maximum, ref taskbar);
        }
    }
}
