#pragma once

#pragma comment(lib, "OneCoreUAP.lib")
#pragma comment(lib, "Dwmapi.lib")

#pragma comment(linker,"\"/manifestdependency:type='win32' \
name='Microsoft.Windows.Common-Controls' version='6.0.0.0' \
processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define NICOLE_API EXTERN_C __declspec(dllexport)

#define FORMAT_ARGUMENT_NULL_MSG(x) "The argument '" #x "' must not be null."
#define FORMAT_ARGUMENT_NULL_OR_EMPTY_MSG(x) "The argument '" #x "' must not be null or empty."

namespace Snap::Nicole::Native::Private
{
    extern HMODULE s_hinstDLL;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved);

#define WINDOWS_10_0_10240_0 1
#define WINDOWS_10_0_10586_0 2
#define WINDOWS_10_0_14393_0 3
#define WINDOWS_10_0_15063_0 4
#define WINDOWS_10_0_16299_0 5
#define WINDOWS_10_0_17134_0 6
#define WINDOWS_10_0_17763_0 7
#define WINDOWS_10_0_18362_0 8
#define WINDOWS_10_0_19041_0 10
#define WINDOWS_10_0_20348_0 12
#define WINDOWS_10_0_22000_0 14
#define WINDOWS_10_0_22621_0 15
#define WINDOWS_10_0_26100_0 19