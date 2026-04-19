#include "framework.h"
#include "NicoleNative.h"
#include <shellapi.h>
#include <string>
#include <wil/registry.h>
#include <windowsx.h>
#include <winrt/Microsoft.UI.Windowing.h>
#include <winrt/Windows.Foundation.Metadata.h>

namespace Snap::Nicole::Native
{
    constexpr UINT_PTR DOUBLE_CLICK_EVENT_ID = 0x1U;

    enum struct NicoleNativeNotifyIconCallbackKind : INT32
    {
        None = 0,
        TaskbarCreated = 1,
        ContextMenu = 2,
        LeftButtonDown = 3,
        LeftButtonDoubleClick = 4,
    };

    namespace Private::NotifyIcon
    {
        static const UINT s_wmTaskbarCreated = RegisterWindowMessageW(L"TaskbarCreated");
        static const UINT s_wmNotifyIconCallback = RegisterWindowMessageW(L"SnapNicoleNotifyIconCallback");
        static wil::srwlock s_lock;
        static std::unordered_map<HWND, std::tuple<NicoleNativeNotifyIconCallback, LPVOID>> s_callbacks;
        static std::unordered_map<HWND, GUID> s_iconIds;

        // This function should swallow all exceptions, when icon key is not found, we treat the notify icon as not promoted
        static void InitializeIconKey(LPCGUID pId, wil::unique_hkey& hKeyIcon)
        {
            hKeyIcon.reset();

            if (!winrt::Windows::Foundation::Metadata::ApiInformation::IsApiContractPresent(L"Windows.Foundation.UniversalApiContract", WINDOWS_10_0_26100_0))
            {
                WCHAR buffer[39];
                if (!StringFromGUID2(*pId, buffer, ARRAYSIZE(buffer)))
                {
                    return;
                }

                std::wstring idString(buffer);
                wil::unique_hkey key;
                if (FAILED(wil::reg::open_unique_key_nothrow(HKEY_CURRENT_USER, L"Control Panel\\NotifyIconSettings", key)))
                {
                    return;
                }

                for (const auto& keyData : wil::make_range(wil::reg::key_iterator(key.get()), wil::reg::key_iterator{}))
                {
                    wil::unique_hkey subKey;
                    if (FAILED(wil::reg::open_unique_key_nothrow(key.get(), keyData.name.c_str(), subKey)))
                    {
                        continue;
                    }

                    wil::unique_cotaskmem_string iconGuid;
                    if (FAILED(wil::reg::get_value_string_nothrow(subKey.get(), L"IconPath", iconGuid)))
                    {
                        continue;
                    }

                    if (CompareStringOrdinal(idString.c_str(), -1, iconGuid.get(), -1, TRUE) == CSTR_EQUAL)
                    {
                        hKeyIcon.reset(subKey.release());
                        break;
                    }
                }
            }
        }

        static HRESULT Add(LPCGUID id, HWND hWnd, LPCWSTR tip, UINT uCallbackMessage, HICON hIcon)
        {
            NOTIFYICONDATAW data
            {
                .cbSize = sizeof(NOTIFYICONDATAW),
                .hWnd = hWnd,
                .uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_STATE | NIF_GUID | NIF_SHOWTIP,
                .uCallbackMessage = uCallbackMessage,
                .hIcon = hIcon,
                .dwStateMask = NIS_HIDDEN,
                .guidItem = *id,
            };

            RETURN_IF_FAILED_MSG(StringCchCopyW(data.szTip, 128, tip), "Failed to copy tip");
            RETURN_LAST_ERROR_IF_MSG(!Shell_NotifyIconW(NIM_ADD, &data), "Failed to add notify icon");
            return S_OK;
        }

        static HRESULT Delete(LPCGUID id)
        {
            NOTIFYICONDATAW data
            {
                .cbSize = sizeof(NOTIFYICONDATAW),
                .uFlags = NIF_GUID,
                .guidItem = *id,
            };

            if (!Shell_NotifyIconW(NIM_DELETE, &data))
            {
                DWORD error = GetLastError();
                if (error == E_FAIL || error == ERROR_TIMEOUT)
                {
                    // E_FAIL means the icon is not added, we can safely return S_OK
                    // ERROR_TIMEOUT means the taskbar is not available, we can also safely return S_OK
                    return S_OK;
                }

                RETURN_IF_WIN32_ERROR_MSG(error, "Failed to delete notify icon");
            }

            return S_OK;
        }

        static HRESULT SetVersion(LPCGUID id, UINT version)
        {
            NOTIFYICONDATAW data
            {
                .cbSize = sizeof(NOTIFYICONDATAW),
                .uFlags = NIF_GUID,
                .uVersion = version,
                .guidItem = *id,
            };

            RETURN_LAST_ERROR_IF_MSG(!Shell_NotifyIconW(NIM_SETVERSION, &data), "Failed to set notify icon's version");
            return S_OK;
        }

        static HRESULT GetRect(LPCGUID id, RECT* rect)
        {
            NOTIFYICONIDENTIFIER identifier
            {
                .cbSize = sizeof(NOTIFYICONIDENTIFIER),
                .guidItem = *id,
            };

            RETURN_IF_FAILED_MSG(Shell_NotifyIconGetRect(&identifier, rect), "Failed to get notify icon's bounding rect");
            return S_OK;
        }

        static RECT GetRectWithFallback(LPCGUID id)
        {
            RECT rect;
            if (FAILED(GetRect(id, &rect)))
            {
                POINT point;
                LOG_IF_WIN32_BOOL_FALSE(GetCursorPos(&point));
                rect = { point.x - 8, point.y - 8, point.x + 8, point.y + 8 };
            }

            return rect;
        }

        static LRESULT CALLBACK WindowProcedure(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
        {
            static bool doubleClicked = false;
            static POINT previousClickPoint = {};

            if (uMsg == WM_CREATE || uMsg == WM_NCCREATE || !s_callbacks.contains(hWnd) || !s_iconIds.contains(hWnd))
            {
                return DefWindowProcW(hWnd, uMsg, wParam, lParam);
            }

            auto lock = s_lock.lock_shared();
            auto& [callback, userData] = s_callbacks[hWnd];
            GUID id = s_iconIds[hWnd];

            if (s_wmTaskbarCreated && uMsg == s_wmTaskbarCreated)
            {
                callback(NicoleNativeNotifyIconCallbackKind::TaskbarCreated, {}, {}, userData);
            }
            else if (s_wmNotifyIconCallback && uMsg == s_wmNotifyIconCallback)
            {
                // https://learn.microsoft.com/zh-cn/windows/win32/api/shellapi/ns-shellapi-notifyicondataw#nif_showtip-0x00000080
                POINT point{ .x = GET_X_LPARAM(wParam), .y = GET_Y_LPARAM(wParam) };
                switch (LOWORD(lParam))
                {
                case WM_CONTEXTMENU:
                    callback(NicoleNativeNotifyIconCallbackKind::ContextMenu, GetRectWithFallback(&id), point, userData);
                    break;
                case WM_LBUTTONDOWN:
                    doubleClicked = false;
                    previousClickPoint = point;
                    SetTimer(hWnd, DOUBLE_CLICK_EVENT_ID, GetDoubleClickTime(), NULL);
                    break;
                case WM_LBUTTONDBLCLK:
                    doubleClicked = true;
                    KillTimer(hWnd, DOUBLE_CLICK_EVENT_ID);
                    callback(NicoleNativeNotifyIconCallbackKind::LeftButtonDoubleClick, GetRectWithFallback(&id), point, userData);
                    break;
                }
            }
            else if (uMsg == WM_TIMER)
            {
                if (wParam == DOUBLE_CLICK_EVENT_ID)
                {
                    KillTimer(hWnd, DOUBLE_CLICK_EVENT_ID);
                    if (!doubleClicked)
                    {
                        callback(NicoleNativeNotifyIconCallbackKind::LeftButtonDown, GetRectWithFallback(&id), previousClickPoint, userData);
                    }
                }
            }

            return DefWindowProcW(hWnd, uMsg, wParam, lParam);
        }
    }

    NicoleNativeNotifyIcon::NicoleNativeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, HRESULT* result)
        : m_hWndMessage(NULL), m_atomWndClass(0), m_iconId(*pId)
    {
        m_hIcon = wil::unique_hicon(static_cast<HICON>(LoadImageW(NULL, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE)));
        if (m_hIcon == NULL)
        {
            *result = HRESULT_FROM_WIN32(GetLastError());
            return;
        }

        Private::NotifyIcon::InitializeIconKey(pId, m_hKeyIcon);
    }

    NicoleNativeNotifyIcon::~NicoleNativeNotifyIcon()
    {
        Destroy();
    }

    HRESULT NicoleNativeNotifyIcon::Create(NicoleNativeNotifyIconCallback callback, LPVOID userData, LPCWSTR tip)
    {
        WNDCLASSW wndClass = { .style = CS_DBLCLKS, .lpfnWndProc = Private::NotifyIcon::WindowProcedure, .lpszClassName = L"SnapNicoleNotifyIconMessageWindowClass" };
        m_atomWndClass = RegisterClassW(&wndClass);
        RETURN_LAST_ERROR_IF_MSG(!m_atomWndClass, "Failed to register window class");

        // Do not use HWND_MESSAGE, we need a top-level window to receive messages from the taskbar
        m_hWndMessage = CreateWindowExW(0, MAKEINTATOM(m_atomWndClass), L"SnapNicoleNotifyIconMessageWindow", WS_OVERLAPPED, 0, 0, 0, 0, NULL, NULL, NULL, NULL);
        RETURN_LAST_ERROR_IF_MSG(!m_hWndMessage, "Failed to create window");

        auto lock = Private::NotifyIcon::s_lock.lock_exclusive();
        Private::NotifyIcon::s_callbacks[m_hWndMessage] = { callback, userData };
        Private::NotifyIcon::s_iconIds[m_hWndMessage] = m_iconId;

        return Recreate(tip);
    }

    HRESULT NicoleNativeNotifyIcon::Recreate(LPCWSTR tip)
    {
        // We can ignore the error of deleting a non-existing icon, since we want to make sure the icon is deleted before adding it again
        Private::NotifyIcon::Delete(&m_iconId);

        // ERROR_NO_TOKEN
        RETURN_IF_FAILED_MSG(Private::NotifyIcon::Add(&m_iconId, m_hWndMessage, tip, Private::NotifyIcon::s_wmNotifyIconCallback, m_hIcon.get()), "Failed to add notify icon");
        RETURN_IF_FAILED_MSG(Private::NotifyIcon::SetVersion(&m_iconId, NOTIFYICON_VERSION_4), "Failed to set notify icon version");

        return S_OK;
    }

    HRESULT NicoleNativeNotifyIcon::Destroy()
    {
        // ERROR_TIMEOUT
        RETURN_IF_FAILED_MSG(Private::NotifyIcon::Delete(&m_iconId), "Failed to delete notify icon");

        HWND hWnd = m_hWndMessage;
        if (hWnd && IsWindow(hWnd))
        {
            RETURN_LAST_ERROR_IF_MSG(!DestroyWindow(hWnd), "Failed to destroy message window");
        }

        if (m_atomWndClass)
        {
            RETURN_LAST_ERROR_IF_MSG(!UnregisterClassW(MAKEINTATOM(m_atomWndClass), NULL), "Failed to unregister window class atom");
            m_atomWndClass = 0;
        }

        if (hWnd)
        {
            auto lock = Private::NotifyIcon::s_lock.lock_exclusive();
            Private::NotifyIcon::s_callbacks.erase(hWnd);
            Private::NotifyIcon::s_iconIds.erase(hWnd);
        }

        return S_OK;
    }

    HRESULT NicoleNativeNotifyIcon::IsPromoted(BOOL* pIsPromoted)
    {
        RETURN_HR_IF_NULL_MSG(E_POINTER, pIsPromoted, FORMAT_ARGUMENT_NULL_MSG(pIsPromoted));
        *pIsPromoted = FALSE;

        HRESULT result;

        if (m_hKeyIcon)
        {
            DWORD isPromotedValue;
            result = wil::reg::get_value_dword_nothrow(m_hKeyIcon.get(), L"IsPromoted", &isPromotedValue);
            if (result == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
            {
                return S_OK;
            }

            RETURN_IF_FAILED_MSG(result, "Failed to get registry value");
            *pIsPromoted = isPromotedValue;
            return S_OK;
        }

        // When explorer.exe is not running, we can get a E_FAIL
        RECT iconRect;

        result = Private::NotifyIcon::GetRect(&m_iconId, &iconRect);
        if (result == E_FAIL)
        {
            return S_OK;
        }

        RETURN_IF_FAILED_MSG(result, "Failed to get icon rect");

        if (winrt::Windows::Foundation::Metadata::ApiInformation::IsApiContractPresent(L"Windows.Foundation.UniversalApiContract", WINDOWS_10_0_22000_0))
        {
            winrt::Windows::Graphics::RectInt32 rectInt32 = winrt::Microsoft::UI::Windowing::DisplayArea::Primary().OuterBounds();
            RECT primaryRect = { .left = rectInt32.X, .top = rectInt32.Y, .right = rectInt32.X + rectInt32.Width, .bottom = rectInt32.Y + rectInt32.Height };
            RECT _;
            *pIsPromoted = IntersectRect(&_, &primaryRect, &iconRect);
        }
        else
        {
            HWND shellTrayWnd = FindWindowExW(NULL, NULL, L"Shell_TrayWnd", NULL);
            RETURN_LAST_ERROR_IF_NULL_MSG(shellTrayWnd, "Failed to find window: 'Shell_TrayWnd'");

            HWND trayNotifyWnd = FindWindowExW(shellTrayWnd, NULL, L"TrayNotifyWnd", NULL);
            RETURN_LAST_ERROR_IF_NULL_MSG(trayNotifyWnd, "Failed to find window: 'TrayNotifyWnd'");

            HWND button = FindWindowExW(trayNotifyWnd, NULL, L"Button", NULL);
            if (button == nullptr)
            {
                DWORD error = GetLastError();
                if (error == ERROR_SUCCESS)
                {
                    // If the Button is not found, it means the notify icon is not promoted to the overflow area, we can directly return true without comparing the rect
                    *pIsPromoted = FALSE;
                    return S_OK;
                }
                else
                {
                    RETURN_WIN32_MSG(error, "Failed to find window: 'Button'");
                }
            }

            RECT buttonRect;
            RETURN_LAST_ERROR_IF_MSG(!GetWindowRect(button, &buttonRect), "Failed to get window rect");
            *pIsPromoted = !EqualRect(&buttonRect, &iconRect);
        }

        return S_OK;
    }
}