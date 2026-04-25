#include <wil/resource.h>
#include <Windows.h>
#include <wrl/implements.h>
#include <thread>

namespace Snap::Nicole::Native
{
    using namespace Microsoft::WRL;

    struct INicoleNative;
    struct INicoleNativeNotifyIcon;
    struct INicoleNativeWindowSubclass;

    class NicoleNativeNotifyIcon;

    enum struct NicoleNativeNotifyIconCallbackKind : INT32;

    using NicoleNativeNotifyIconCallback = void(*)(NicoleNativeNotifyIconCallbackKind kind, RECT icon, POINT point, LPVOID userData);
    using NicoleNativeWindowSubclassCallback = BOOL(*)(HWND hWnd, UINT32 uMsg, WPARAM wParam, LPARAM lParam, LPCVOID userData, LRESULT* pResult);

#pragma region INicoleNative
    MIDL_INTERFACE("E5EEEB3A-C782-4C90-8F93-91830D7F1F58") INicoleNative : public IUnknown
    {
        virtual HRESULT APIENTRY MakeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, INicoleNativeNotifyIcon * *ppv) = 0;
        virtual HRESULT APIENTRY MakeWindowSubclass(HWND hWnd, NicoleNativeWindowSubclassCallback callback, LPVOID userData, INicoleNativeWindowSubclass** ppv) = 0;
    };

    class NicoleNative : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IAgileObject, INicoleNative>
    {
        HRESULT APIENTRY MakeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, INicoleNativeNotifyIcon** ppv) override;
        HRESULT APIENTRY MakeWindowSubclass(HWND hWnd, NicoleNativeWindowSubclassCallback callback, LPVOID userData, INicoleNativeWindowSubclass** ppv) override;
    };
#pragma endregion

#pragma region INicoleNativeNotifyIcon
    MIDL_INTERFACE("6F37022E-238B-426D-8C91-07A431B00FAC") INicoleNativeNotifyIcon : public IUnknown
    {
        virtual HRESULT APIENTRY Create(NicoleNativeNotifyIconCallback callback, LPVOID userData, LPCWSTR tip) = 0;
        virtual HRESULT APIENTRY Recreate(LPCWSTR tip) = 0;
        virtual HRESULT APIENTRY Destroy() = 0;
        virtual HRESULT APIENTRY IsPromoted(BOOL* pIsPromoted) = 0;
    };

    class NicoleNativeNotifyIcon : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IAgileObject, INicoleNativeNotifyIcon>
    {
    private:
        wil::unique_hicon m_hIcon;
        wil::unique_hkey m_hKeyIcon;
        ATOM m_atomWndClass;
        HWND m_hWndMessage;
        GUID m_iconId;
    public:
        NicoleNativeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, HRESULT* result);
        ~NicoleNativeNotifyIcon();
        HRESULT APIENTRY Create(NicoleNativeNotifyIconCallback callback, LPVOID userData, LPCWSTR tip) override;
        HRESULT APIENTRY Recreate(LPCWSTR tip) override;
        HRESULT APIENTRY Destroy() override;
        HRESULT APIENTRY IsPromoted(BOOL* pIsPromoted) override;
    };
#pragma endregion

#pragma region INicoleNativeWindowSubclass
    MIDL_INTERFACE("2F14C477-E0CF-40D5-BBA4-7DA17D63C736") INicoleNativeWindowSubclass : public IUnknown
    {
        virtual HRESULT APIENTRY Attach() = 0;
        virtual HRESULT APIENTRY Detach() = 0;
    };

    class NicoleNativeWindowSubclass : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IAgileObject, INicoleNativeWindowSubclass>
    {
    private:
        const HWND m_hWnd;
        const NicoleNativeWindowSubclassCallback m_callback;
        const LPVOID m_userData;
        std::atomic<BOOL> m_attached;
        BOOL m_isCompositionEnabled{ FALSE };
    public:
        NicoleNativeWindowSubclass(HWND hWnd, NicoleNativeWindowSubclassCallback callback, LPVOID userData) :
            m_hWnd(hWnd), m_callback(callback), m_userData(userData), m_attached(FALSE)
        {};
        BOOL InvokeCallback(HWND hWnd, UINT32 uMsg, WPARAM wParam, LPARAM lParam, LRESULT* result) const;
        BOOL IsDesktopCompositionEnabled() const;
        HRESULT APIENTRY Attach() override;
        HRESULT APIENTRY Detach() override;
    };
#pragma endregion
}

NICOLE_API HRESULT APIENTRY NicoleCreateInstance(Snap::Nicole::Native::INicoleNative** ppv);