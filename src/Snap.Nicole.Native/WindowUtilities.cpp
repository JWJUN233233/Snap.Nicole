#include "WindowUtilities.h"
#include <cmath>
#include <wil/result.h>
#include <wrl.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Microsoft.UI.Windowing.h>

HRESULT WindowUtilitiesAppWindowEnablePlacementRestoration(winrt::Microsoft::UI::WindowId windowId, GUID guid)
{
    // using namespace winrt::Windows::Foundation;
    // using namespace winrt::Microsoft::UI::Windowing;
    // AppWindow appWindow = AppWindow::GetFromWindowId(windowId);
    // appWindow.PlacementRestorationBehavior(PlacementRestorationBehavior::All);
    // winrt::guid winrtGuid(guid);
    // IReference<winrt::guid> guidReference{ winrtGuid };
    // appWindow.PersistedStateId(guidReference);
    // return S_OK;
    using namespace winrt;
    using namespace winrt::Microsoft::UI::Windowing;
    using namespace winrt::Windows::Foundation;

    AppWindow appWindow = AppWindow::GetFromWindowId(windowId);

    winrt::guid winrtGuid(guid);
    IReference<winrt::guid> guidReference{ winrtGuid };

    constexpr winrt::guid iid{ 0x04DB96C7,0xDEB6,0x5BE4,{ 0xBF,0xDC,0x1B,0xC0,0x36,0x1C,0x8A,0x12 } };
    com_ptr<::IUnknown> raw{};
    RETURN_IF_FAILED(reinterpret_cast<::IUnknown*>(get_abi(appWindow))->QueryInterface(iid, put_abi(raw)));

    void** vtable = *reinterpret_cast<void***>(get_abi(raw));
    RETURN_IF_FAILED((reinterpret_cast<HRESULT(__stdcall*)(void*, UINT32)>(vtable[9])(get_abi(raw), 0xFFFFFFFF)));
    RETURN_IF_FAILED((reinterpret_cast<HRESULT(__stdcall*)(void*, void*)>(vtable[7])(get_abi(raw), get_abi(guidReference))));
    return S_OK;
}

HRESULT WindowUtilitiesSwitchToWindow(HWND hWnd)
{
    if (!IsWindow(hWnd))
    {
        return S_OK;
    }

    if (!IsWindowVisible(hWnd))
    {
        ShowWindow(hWnd, SW_SHOW);
    }

    if (IsIconic(hWnd))
    {
        ShowWindow(hWnd, SW_RESTORE);
    }

    if (!SetForegroundWindow(hWnd))
    {
        FLASHWINFO info
        {
            .cbSize = sizeof(FLASHWINFO),
            .hwnd = hWnd,
            .dwFlags = FLASHW_TRAY | FLASHW_TIMERNOFG,
        };
        LOG_IF_WIN32_BOOL_FALSE(FlashWindowEx(&info));
    }

    return S_OK;
}

HRESULT WindowUtilitiesAddExtendedStyleLayered(HWND hWnd)
{
    if (!IsWindow(hWnd))
    {
        return S_OK;
    }

    SetLastError(ERROR_SUCCESS);

    LONG_PTR style = GetWindowLongPtrW(hWnd, GWL_EXSTYLE);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to get window extended style");

    // ERROR_DLL_INIT_FAILED
    SetWindowLongPtrW(hWnd, GWL_EXSTYLE, style | WS_EX_LAYERED);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to set window extended style");

    if (!SetWindowPos(hWnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED))
    {
        DWORD error = GetLastError();
        if (error == ERROR_INVALID_PARAMETER)
        {
            // Window is closing
            return S_OK;
        }

        RETURN_WIN32_MSG(error, "Failed to update window");
    }

    return S_OK;
}

HRESULT WindowUtilitiesRemoveExtendedStyleLayered(HWND hWnd)
{
    if (!IsWindow(hWnd))
    {
        return S_OK;
    }

    SetLastError(ERROR_SUCCESS);

    LONG_PTR style = GetWindowLongPtrW(hWnd, GWL_EXSTYLE);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to get window extended style");

    SetWindowLongPtrW(hWnd, GWL_EXSTYLE, style & ~WS_EX_LAYERED);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to set window extended style");

    RETURN_IF_WIN32_BOOL_FALSE_MSG(SetWindowPos(hWnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED), "Failed to update window");
    return S_OK;
}

HRESULT WindowUtilitiesSetLayeredWindowTransparency(HWND hWnd, BYTE opacity)
{
    if (!IsWindow(hWnd))
    {
        return S_OK;
    }

    RETURN_IF_WIN32_BOOL_FALSE_MSG(SetLayeredWindowAttributes(hWnd, RGB(0, 0, 0), opacity, LWA_COLORKEY | LWA_ALPHA), "Failed to set layered window transparency");
    return S_OK;
}

HRESULT WindowUtilitiesAddExtendedStyleToolWindow(HWND hWnd)
{
    if (!IsWindow(hWnd))
    {
        return S_OK;
    }

    SetLastError(ERROR_SUCCESS);

    LONG_PTR style = GetWindowLongPtrW(hWnd, GWL_EXSTYLE);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to get window extended style");

    SetWindowLongPtrW(hWnd, GWL_EXSTYLE, style | WS_EX_TOOLWINDOW);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to set window extended style");

    RETURN_IF_WIN32_BOOL_FALSE_MSG(SetWindowPos(hWnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED), "Failed to update window");
    return S_OK;
}

HRESULT WindowUtilitiesRemoveStyleOverlappedWindow(HWND hWnd)
{
    if (!IsWindow(hWnd))
    {
        return S_OK;
    }

    SetLastError(ERROR_SUCCESS);

    LONG_PTR style = GetWindowLongPtrW(hWnd, GWL_STYLE);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to get window style");

    SetWindowLongPtrW(hWnd, GWL_STYLE, style & ~WS_OVERLAPPEDWINDOW);
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to get window style");

    RETURN_IF_WIN32_BOOL_FALSE_MSG(SetWindowPos(hWnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED), "Failed to update window");
    return S_OK;
}

HRESULT WindowUtilitiesGetRasterizationScaleForWindow(HWND hWnd, FLOAT* scale)
{
    RETURN_HR_IF_NULL_MSG(E_POINTER, scale, FORMAT_ARGUMENT_NULL_MSG(scale));

    UINT dpi = GetDpiForWindow(hWnd);
    RETURN_HR_IF(E_INVALIDARG, 0 == dpi);

    *scale = std::floor((dpi * 100.0f / 96.0f) + 0.5f) / 100.0f;
    return S_OK;
}

HRESULT WindowUtilitiesSetWindowIsEnabled(HWND hWnd, BOOL isEnabled)
{
    EnableWindow(hWnd, isEnabled);
    return S_OK;
}

HRESULT WindowUtilitiesSetWindowOwner(HWND hWnd, HWND hWndOwner)
{
    SetLastError(ERROR_SUCCESS);

    SetWindowLongPtrW(hWnd, GWLP_HWNDPARENT, reinterpret_cast<LONG_PTR>(hWndOwner));
    RETURN_IF_WIN32_ERROR_MSG(GetLastError(), "Failed to set window owner");

    return S_OK;
}

HRESULT WindowUtilitiesSetTaskbarProgress(HWND hWnd, TBPFLAG state, ULONG64 value, ULONG64 maximum, ITaskbarList3** ppTaskbar)
{
    RETURN_HR_IF_NULL_MSG(E_POINTER, ppTaskbar, FORMAT_ARGUMENT_NULL_MSG(ppTaskbar));

    Microsoft::WRL::ComPtr<ITaskbarList3> taskbar;
    taskbar.Attach(*ppTaskbar);
    auto detachTaskbar = wil::scope_exit([&]
        {
            *ppTaskbar = taskbar.Detach();
        });

    if (!taskbar)
    {
        RETURN_IF_FAILED_MSG(CoCreateInstance(__uuidof(TaskbarList), nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&taskbar)), "Failed to create TaskbarList");
        RETURN_IF_FAILED_MSG(taskbar->HrInit(), "Failed to initialize TaskbarList");
    }

    RETURN_IF_FAILED_MSG(taskbar->SetProgressState(hWnd, state), "Failed to set progress state");
    if (!WI_IsAnyFlagSet(state, TBPF_NOPROGRESS | TBPF_INDETERMINATE))
    {
        RETURN_IF_FAILED_MSG(taskbar->SetProgressValue(hWnd, value, maximum), "Failed to set progress value");
    }

    return S_OK;
}