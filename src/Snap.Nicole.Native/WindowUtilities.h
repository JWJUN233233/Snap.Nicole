#include "framework.h"
#include <ShlObj_core.h>

NICOLE_API HRESULT APIENTRY WindowUtilitiesSwitchToWindow(HWND hWnd);

NICOLE_API HRESULT APIENTRY WindowUtilitiesAddExtendedStyleLayered(HWND hWnd);

NICOLE_API HRESULT APIENTRY WindowUtilitiesRemoveExtendedStyleLayered(HWND hWnd);

NICOLE_API HRESULT APIENTRY WindowUtilitiesSetLayeredWindowTransparency(HWND hWnd, BYTE opacity);

NICOLE_API HRESULT APIENTRY WindowUtilitiesAddExtendedStyleToolWindow(HWND hWnd);

NICOLE_API HRESULT APIENTRY WindowUtilitiesRemoveStyleOverlappedWindow(HWND hWnd);

NICOLE_API HRESULT APIENTRY WindowUtilitiesGetRasterizationScaleForWindow(HWND hWnd, FLOAT* scale);

NICOLE_API HRESULT APIENTRY WindowUtilitiesSetWindowIsEnabled(HWND hWnd, BOOL isEnabled);

NICOLE_API HRESULT APIENTRY WindowUtilitiesSetWindowOwner(HWND hWnd, HWND parentHWnd);

NICOLE_API HRESULT APIENTRY WindowUtilitiesSetTaskbarProgress(HWND hWnd, TBPFLAG state, ULONG64 value, ULONG64 maximum, ITaskbarList3** ppTaskbar);