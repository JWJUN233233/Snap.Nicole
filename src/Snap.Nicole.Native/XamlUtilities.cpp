#include "XamlUtilities.h"
#include "Detours/detours.h"
#include <string>
#include <wil/result.h>
#include <algorithm>
#include <mutex>
#include <span>

using XStringPtrCloneBufferFunction = HRESULT(*)(const WCHAR*, void*);

static HMODULE s_ModuleHandle;
static std::wstring s_ResourceString;
static LPVOID s_pFnPALFontAndScriptServicesGetDefaultFontNameString;
static XStringPtrCloneBufferFunction s_pFnXStringPtrCloneBuffer;

static HRESULT Endpoint(LPCVOID /* PALFontAndScriptServices* */ _this, void* /* xstring_ptr* */ pstrDefaultFontNameString)
{
    RETURN_IF_FAILED(s_pFnXStringPtrCloneBuffer(s_ResourceString.c_str(), pstrDefaultFontNameString));
    return S_OK;
}

static HRESULT GetSpan(LPBYTE hModule, std::span<BYTE>* span)
{
    IMAGE_DOS_HEADER* pImageDosHeader = reinterpret_cast<IMAGE_DOS_HEADER*>(hModule);
    IMAGE_NT_HEADERS64* pImageNtHeader = reinterpret_cast<IMAGE_NT_HEADERS64*>(hModule + pImageDosHeader->e_lfanew);

    *span = std::span<BYTE>(hModule, pImageNtHeader->OptionalHeader.SizeOfImage);
    return S_OK;
}

static HRESULT LocatePALFontAndScriptServicesGetDefaultFontNameString(std::span<BYTE> memory, LPVOID* func)
{
    // __int64 __fastcall PALFontAndScriptServices::GetDefaultFontNameString(PALFontAndScriptServices *this, xstring_ptr *pstrDefaultFontNameString)
    // 48 89 5C 24 18                          mov     [rsp-8+arg_10], rbx
    // 48 89 74 24 20                          mov     [rsp - 8 + arg_18], rsi
    // 55                                      push    rbp
    // 57                                      push    rdi
    // 41 54                                   push    r12
    // 41 56                                   push    r14
    // 41 57                                   push    r15
    // 48 8D AC 24 30 FE FF FF                 lea     rbp, [rsp - 1D0h]
    // 48 81 EC D0 02 00 00                    sub     rsp, 2D0h
    // 48 8B 05 ?? ?? ?? ??                    mov     rax, cs:__security_cookie
    constexpr BYTE pattern[] =
    {
        0x48, 0x89, 0x5C, 0x24, 0x18,
        0x48, 0x89, 0x74, 0x24, 0x20,
        0x55,
        0x57,
        0x41, 0x54,
        0x41, 0x56,
        0x41, 0x57,
        0x48, 0x8D, 0xAC, 0x24, 0x30, 0xFE, 0xFF, 0xFF,
        0x48, 0x81, 0xEC, 0xD0, 0x02, 0x00, 0x00,
        0x48, 0x8B, 0x05
    };

    std::span<BYTE>::iterator it = std::search(memory.begin(), memory.end(), std::begin(pattern), std::end(pattern));
    RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_NOT_FOUND), it == memory.end(), "Failed to locate PALFontAndScriptServices::GetDefaultFontNameString");
    *func = reinterpret_cast<LPVOID>(memory.data() + std::distance(memory.begin(), it));
    return S_OK;
}

static HRESULT LocateXStringPtrCloneBuffer(std::span<BYTE> memory, XStringPtrCloneBufferFunction* func)
{
    // HRESULT __fastcall xstring_ptr::CloneBuffer(const wchar_t *buffer, xstring_ptr *pstrCloned)
    // 48 83 EC 28                             sub     rsp, 28h
    // 4C 8B C2                                mov     r8, pstrCloned
    // 4C 8B C9                                mov     r9, buffer
    // E8 ?? ?? ?? ??                          call    ?xstrlen@@YAIPEBG@Z ; xstrlen(ushort const *)
    // 8B D0                                   mov     edx, eax
    // 49 8B C9                                mov     buffer, r9
    // 48 83 C4 28                             add     rsp, 28h
    // E9 ?? ?? ?? ??                          jmp     ?CloneBuffer@xstring_ptr@@SAJPEBGIPEAV1@@Z ;
    constexpr BYTE p1[] =
    {
        0x48, 0x83, 0xEC, 0x28,
        0x4C, 0x8B, 0xC2,
        0x4C, 0x8B, 0xC9,
        0xE8
    };
    const BYTE p2[] =
    {
        0x8B, 0xD0,
        0x49, 0x8B, 0xC9,
        0x48, 0x83, 0xC4, 0x28,
        0xE9
    };

    std::ptrdiff_t offset = 0;
    while (offset < memory.size())
    {
        std::span<BYTE>::iterator it = std::search(std::next(memory.begin(), offset), memory.end(), std::begin(p1), std::end(p1));
        RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_NOT_FOUND), it == memory.end(), "Failed to locate xstring_ptr::CloneBuffer");
        offset = std::distance(memory.begin(), it);

        std::span<BYTE> part1 = memory.subspan(offset);
        std::span<BYTE> part2 = part1.subspan(sizeof(p1) + 4);
        if (!part2.empty() && std::equal(std::begin(p2), std::end(p2), part2.begin()))
        {
            *func = reinterpret_cast<XStringPtrCloneBufferFunction>(memory.data() + offset);
            return S_OK;
        }

        offset += sizeof(p1) + 4 + sizeof(p2);
    }

    RETURN_HR_MSG(HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "Failed to locate xstring_ptr::CloneBuffer");
}

HRESULT XamlUtilitiesPatchFontAndScriptServicesGetDefaultFontNameString(LPCWSTR pResource)
{
    static std::once_flag locateFlag;
    static HRESULT result;

    std::call_once(locateFlag, [&]()
        {
            result = [&]()
                {
                    s_ModuleHandle = GetModuleHandleW(L"Microsoft.UI.Xaml.dll");
                    RETURN_LAST_ERROR_IF_NULL_MSG(s_ModuleHandle, "Failed to get handle of microsoft.ui.xaml.dll");
                    std::span<BYTE> moduleMemory;
                    RETURN_IF_FAILED_MSG(GetSpan(reinterpret_cast<LPBYTE>(s_ModuleHandle), &moduleMemory), "Failed to get module memory span");
                    RETURN_IF_FAILED_MSG(LocatePALFontAndScriptServicesGetDefaultFontNameString(moduleMemory, &s_pFnPALFontAndScriptServicesGetDefaultFontNameString), "Failed when calling LocatePALFontAndScriptServicesGetDefaultFontNameString");
                    RETURN_IF_FAILED_MSG(LocateXStringPtrCloneBuffer(moduleMemory, &s_pFnXStringPtrCloneBuffer), "Failed when calling LocateXStringPtrCloneBuffer");
                    return S_OK;
                }();
        });

    RETURN_IF_FAILED_MSG(result, "XamlUtilitiesPatchFontAndScriptServicesGetDefaultFontNameString initialization failed");

    s_ResourceString = pResource;

    RETURN_IF_WIN32_ERROR(DetourTransactionBegin());
    RETURN_IF_WIN32_ERROR(DetourUpdateThread(GetCurrentThread()));
    RETURN_IF_WIN32_ERROR(DetourAttach(&s_pFnPALFontAndScriptServicesGetDefaultFontNameString, &Endpoint));
    RETURN_IF_WIN32_ERROR(DetourTransactionCommit());

    return S_OK;
}
