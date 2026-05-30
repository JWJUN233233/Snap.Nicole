#include "framework.h"
#include "NicoleNative.h"
#include <cstddef>
#include <vector>

namespace Snap::Nicole::Native
{
#pragma pack(push, 1)
    // https://learn.microsoft.com/zh-cn/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemfirmwaretable
    struct RawSMBIOSData
    {
        BYTE Used20CallingMethod;
        BYTE SmbiosMajorVersion;
        BYTE SmbiosMinorVersion;
        BYTE DmiRevision;
        DWORD Length;
        BYTE SmbiosTableData[ANYSIZE_ARRAY];
    };

    // https://www.dmtf.org/sites/default/files/standards/documents/DSP0134_3.9.0.pdf
    struct SMBIOSHeader
    {
        BYTE Type;
        BYTE Length;
        WORD Handle;
    };

    struct SMBIOSSystemInformation
    {
        SMBIOSHeader Header;
        BYTE Manufacturer;
        BYTE ProductName;
        BYTE Version;
        BYTE SerialNumber;
        GUID Uuid;
        BYTE WakeUpType;
        BYTE SKUNumber;
        BYTE Family;
    };
#pragma pack(pop)

    HRESULT NicoleNativeFirmwareUuidReader::GetFirmwareUuid(GUID* pUuid)
    {
        RETURN_HR_IF_NULL_MSG(E_POINTER, pUuid, FORMAT_ARGUMENT_NULL_MSG(pUuid));

        UINT requiredSize = GetSystemFirmwareTable('RSMB', 0, NULL, 0);
        RETURN_LAST_ERROR_IF_MSG(requiredSize == 0, "Failed to get SMBIOS firmware table size");

        std::vector<BYTE> tableBuffer(requiredSize);
        UINT writtenSize = GetSystemFirmwareTable('RSMB', 0, tableBuffer.data(), requiredSize);
        RETURN_LAST_ERROR_IF_MSG(writtenSize == 0, "Failed to get SMBIOS firmware table");

        constexpr size_t tableOffset = offsetof(RawSMBIOSData, SmbiosTableData);
        size_t writtenSizeValue = writtenSize;
        RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_DATA), writtenSizeValue < tableOffset, "SMBIOS firmware table is smaller than its header");

        const RawSMBIOSData* rawSmbiosData = reinterpret_cast<RawSMBIOSData*>(tableBuffer.data());
        RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_DATA), rawSmbiosData->Length > writtenSizeValue - tableOffset, "SMBIOS firmware table length exceeds returned buffer size");

        const BYTE* begin = rawSmbiosData->SmbiosTableData;
        const BYTE* end = begin + rawSmbiosData->Length;

        const BYTE* current = begin;
        while (current + sizeof(SMBIOSHeader) <= end)
        {
            const SMBIOSHeader* header = reinterpret_cast<const SMBIOSHeader*>(current);
            RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_DATA), header->Length < sizeof(SMBIOSHeader), "SMBIOS structure header length is invalid");
            RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_DATA), current + header->Length > end, "SMBIOS structure exceeds table bounds");

            if (header->Type == 127) // End-of-table indicator
            {
                break;
            }

            if (header->Type == 1) // System Information
            {
                RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_NOT_FOUND), header->Length < sizeof(SMBIOSSystemInformation), "SMBIOS system information structure does not contain UUID");
                *pUuid = reinterpret_cast<const SMBIOSSystemInformation*>(current)->Uuid;
                return S_OK;
            }

            // Skipping string-set: find the double null terminator after the formatted section
            // Each string is terminated with a null (00h) BYTE and the set of strings is terminated with an additional null(00h) BYTE
            // For more details see 6.1.3 Text strings6.1.3 Text strings
            const BYTE* next = nullptr;
            const BYTE* strings = current + header->Length;
            while (strings + 1 < end)
            {
                if (strings[0] == '\0' && strings[1] == '\0')
                {
                    next = strings + 2;
                    break;
                }

                strings++;
            }

            RETURN_HR_IF_NULL_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_DATA), next, "Failed to locate next SMBIOS structure");

            current = next;
        }

        RETURN_HR_MSG(HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "Failed to find SMBIOS system information structure");
    }
}
