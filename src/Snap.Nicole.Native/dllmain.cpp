#include <Windows.h>

namespace Snap::Nicole::Native::Private
{
    HINSTANCE s_hinstDLL = nullptr;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved);
    if (hModule)
    {
        DisableThreadLibraryCalls(hModule);
        Snap::Nicole::Native::Private::s_hinstDLL = hModule;
    }

    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }

    return TRUE;
}
