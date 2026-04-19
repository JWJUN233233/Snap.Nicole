#include <Windows.h>
#include <wil/resource.h>
#include <wrl/implements.h>

namespace Snap::Nicole::Native
{
    using namespace Microsoft::WRL;

    struct INicoleNative;
    struct INicoleNativeNotifyIcon;
    class NicoleNativeNotifyIcon;

    enum struct NicoleNativeNotifyIconCallbackKind : INT32;

    using NicoleNativeNotifyIconCallback = void(*)(NicoleNativeNotifyIconCallbackKind kind, RECT icon, POINT point, LPVOID userData);

    MIDL_INTERFACE("E5EEEB3A-C782-4C90-8F93-91830D7F1F58") INicoleNative : public IUnknown
    {
        virtual HRESULT APIENTRY MakeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, INicoleNativeNotifyIcon** ppv) = 0;
    };

    class NicoleNative : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IAgileObject, INicoleNative>
    {
        HRESULT APIENTRY MakeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, INicoleNativeNotifyIcon** ppv) override;
    };

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
}

NICOLE_API HRESULT APIENTRY NicoleCreateInstance(Snap::Nicole::Native::INicoleNative** ppv);