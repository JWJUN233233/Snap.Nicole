#include "framework.h"
#include "NicoleNative.h"
#include <wrl/implements.h>

namespace Snap::Nicole::Native
{
    HRESULT NicoleNative::MakeNotifyIcon(LPCWSTR iconPath, LPCGUID pId, INicoleNativeNotifyIcon** ppv)
    {
        RETURN_HR_IF_NULL_MSG(E_POINTER, ppv, FORMAT_ARGUMENT_NULL_MSG(ppv));

        HRESULT result = S_OK;
        RETURN_IF_FAILED_MSG(Make<NicoleNativeNotifyIcon>(iconPath, pId, &result).CopyTo(ppv), "Failed to make NicoleNativeNotifyIcon");
        RETURN_IF_FAILED_MSG(result, "The ctor of NicoleNativeNotifyIcon failed");
        return S_OK;
    }
}

using namespace Microsoft::WRL;

HRESULT NicoleCreateInstance(Snap::Nicole::Native::INicoleNative** ppv)
{
    RETURN_HR_IF_NULL_MSG(E_POINTER, ppv, FORMAT_ARGUMENT_NULL_MSG(ppv));
    RETURN_IF_FAILED_MSG(Make<Snap::Nicole::Native::NicoleNative>().CopyTo(ppv), "Failed to make NicoleNative");
    return S_OK;
}