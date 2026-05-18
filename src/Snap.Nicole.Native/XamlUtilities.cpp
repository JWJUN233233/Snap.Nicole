#include "XamlUtilities.h"
#include "Detours/detours.h"
#include <string>
#include <wil/result.h>

static HMODULE moduleHandle = GetModuleHandleW(L"Microsoft.UI.Xaml.dll");
static std::wstring s_resourceString;

static HRESULT Endpoint(LPCVOID /* PALFontAndScriptServices* */ _this, void* /* xstring_ptr* */ pstrDefaultFontNameString)
{
    // HRESULT __fastcall xstring_ptr::CloneBuffer(const wchar_t *buffer, xstring_ptr *pstrCloned)
    using CloneBufferFunc = HRESULT(*)(const WCHAR*, void*);
    CloneBufferFunc cloneBuffer = reinterpret_cast<CloneBufferFunc>(reinterpret_cast<BYTE*>(moduleHandle) + 0x3B260C);
    RETURN_IF_FAILED(cloneBuffer(s_resourceString.c_str(), pstrDefaultFontNameString));

    return S_OK;
}

HRESULT XamlUtilitiesPatchFontAndScriptServicesGetDefaultFontNameString(LPCWSTR pResource)
{
    s_resourceString = pResource;

    RETURN_IF_WIN32_ERROR(DetourTransactionBegin());
    RETURN_IF_WIN32_ERROR(DetourUpdateThread(GetCurrentThread()));

    // __int64 __fastcall PALFontAndScriptServices::GetDefaultFontNameString(PALFontAndScriptServices *this, xstring_ptr *pstrDefaultFontNameString)
    LPVOID target = reinterpret_cast<LPVOID>(reinterpret_cast<BYTE*>(moduleHandle) + 0x191C90);
    RETURN_IF_WIN32_ERROR(DetourAttach(&target, &Endpoint));
    RETURN_IF_WIN32_ERROR(DetourTransactionCommit());

    return S_OK;
}
