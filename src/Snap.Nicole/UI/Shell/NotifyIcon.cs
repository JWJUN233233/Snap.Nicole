using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace Snap.Nicole.UI.Shell;

internal sealed class NotifyIcon
{
    private static unsafe void Add(Guid id, HWND hWnd, string tip, uint callbackId, HICON icon)
    {
        NOTIFYICONDATAW data = new()
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = hWnd,
            uFlags =
                NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE |
                NOTIFY_ICON_DATA_FLAGS.NIF_ICON |
                NOTIFY_ICON_DATA_FLAGS.NIF_TIP |
                NOTIFY_ICON_DATA_FLAGS.NIF_STATE |
                NOTIFY_ICON_DATA_FLAGS.NIF_GUID |
                NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP,
            uCallbackMessage = callbackId,
            hIcon = icon,
            szTip = tip,
            dwStateMask = NOTIFY_ICON_STATE.NIS_HIDDEN,
            guidItem = id,
        };

        if (!PInvoke.Shell_NotifyIconW(NOTIFY_ICON_MESSAGE.NIM_ADD, &data))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetLastSystemError());
        }
    }

    private static unsafe void Delete(Guid id)
    {
        NOTIFYICONDATAW data = new()
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_GUID,
            guidItem = id,
        };

        if (!PInvoke.Shell_NotifyIconW(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data))
        {
            int lastError = Marshal.GetLastSystemError();
            if (lastError == HRESULT.E_FAIL || lastError == (int)WIN32_ERROR.ERROR_TIMEOUT)
            {
                // E_FAIL means the icon is not added, we can safely return
                // ERROR_TIMEOUT means the taskbar is not available, we can also safely return
                return;
            }

            Marshal.ThrowExceptionForHR(lastError);
        }
    }

    private static unsafe void SetVersion(Guid id, uint version)
    {
        NOTIFYICONDATAW data = new()
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_GUID,
            guidItem = id,
        };

        data.Anonymous.uVersion  = version;

        if (!PInvoke.Shell_NotifyIconW(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, &data))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetLastSystemError());
        }
    }

    private static unsafe void SetFocus(Guid id)
    {
        NOTIFYICONDATAW data = new()
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_GUID,
            guidItem = id,
        };

        if (!PInvoke.Shell_NotifyIconW(NOTIFY_ICON_MESSAGE.NIM_SETFOCUS, &data))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetLastSystemError());
        }
    }
}

internal interface INotifyIcon
{

}