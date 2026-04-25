#include "framework.h"
#include "NicoleNative.h"
#include <dwmapi.h>
#include <commctrl.h>

constexpr UINT_PTR WINDOW_SUBCLASS_ID = 101;

namespace Snap::Nicole::Native
{
    namespace Private::WindowSubclass
    {
        static LRESULT CALLBACK SubclassProcedure(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam, UINT_PTR uIdSubclass, DWORD_PTR dwRefData)
        {
            UNREFERENCED_PARAMETER(uIdSubclass);
            NicoleNativeWindowSubclass* subclass = reinterpret_cast<NicoleNativeWindowSubclass*>(dwRefData);

            if (uMsg == WM_PAINT)
            {
                if (subclass->IsDesktopCompositionEnabled())
                {
                    HRESULT hr = DwmFlush();
                    if (hr != DWM_E_COMPOSITIONDISABLED && hr != HRESULT_FROM_WIN32(ERROR_TIMEOUT) && hr != 0xD0000701)
                    {
                        LOG_IF_FAILED_MSG(hr, "Failed to perform DwmFlush in WM_PAINT");
                    }
                }
            }

            LRESULT result;
            if (subclass->InvokeCallback(hWnd, uMsg, wParam, lParam, &result))
            {
                return DefSubclassProc(hWnd, uMsg, wParam, lParam);
            }

            return result;
        }
    }

    HRESULT NicoleNativeWindowSubclass::Attach()
    {
        if (!m_attached.load(std::memory_order_seq_cst))
        {
            RETURN_IF_FAILED_MSG(DwmIsCompositionEnabled(&m_isCompositionEnabled), "Failed to determine whether desktop composition is enabled");
            RETURN_IF_WIN32_BOOL_FALSE_MSG(SetWindowSubclass(m_hWnd, &Private::WindowSubclass::SubclassProcedure, WINDOW_SUBCLASS_ID, reinterpret_cast<DWORD_PTR>(this)), "Failed to set window subclass");
            m_attached.store(TRUE);
        }

        return S_OK;
    }

    HRESULT NicoleNativeWindowSubclass::Detach()
    {
        if (m_attached.load(std::memory_order_seq_cst))
        {
            if (IsWindow(m_hWnd))
            {
                RETURN_HR_IF_MSG(E_FAIL, !RemoveWindowSubclass(m_hWnd, &Private::WindowSubclass::SubclassProcedure, WINDOW_SUBCLASS_ID), "Failed to remove window subclass");
            }

            m_attached = FALSE;
        }

        return S_OK;
    }

    BOOL NicoleNativeWindowSubclass::InvokeCallback(HWND hWnd, UINT32 uMsg, WPARAM wParam, LPARAM lParam, LRESULT* result) const
    {
        return m_callback ? m_callback(hWnd, uMsg, wParam, lParam, m_userData, result) : FALSE;
    }

    BOOL NicoleNativeWindowSubclass::IsDesktopCompositionEnabled() const
    {
        return m_isCompositionEnabled;
    }
}