#define UNICODE
#define _UNICODE
#include <windows.h>
#include <stdio.h>
#include <wchar.h>
#include "LauncherCore.h"

#define WINDOW_CLASS L"NroLauncherNative"
#define ID_OPEN_OTHER 200
#define ID_SLOT_INPUT 201
#define TIMER_RUNNING 1

static wchar_t gameDir[MAX_PATH];
static wchar_t gameExe[MAX_PATH];
static HANDLE running[1000];
static HWND statusLabel;
static HWND slotInput;

static HWND AddControl(HWND parent, const wchar_t *className, const wchar_t *caption,
                       DWORD style, int x, int y, int width, int height, int id)
{
    HWND control = CreateWindowExW(0, className, caption, WS_CHILD | WS_VISIBLE | style,
                                   x, y, width, height, parent, (HMENU)(INT_PTR)id,
                                   GetModuleHandleW(NULL), NULL);
    if (control != NULL)
        SendMessageW(control, WM_SETFONT, (WPARAM)GetStockObject(DEFAULT_GUI_FONT), TRUE);
    return control;
}

static void RefreshRunning(void)
{
    int count = 0;
    for (int slot = 1; slot <= 999; slot++)
    {
        if (running[slot] == NULL)
            continue;
        if (WaitForSingleObject(running[slot], 0) == WAIT_TIMEOUT)
            count++;
        else
        {
            CloseHandle(running[slot]);
            running[slot] = NULL;
        }
    }
    wchar_t message[128];
    _snwprintf(message, 128, L"Đang chạy từ launcher này: %d phiên.", count);
    SetWindowTextW(statusLabel, message);
}

static void OpenSlot(HWND window, int slot)
{
    if (!NroIsValidSlot(slot))
    {
        MessageBoxW(window, L"Số phiên phải từ 1 đến 999.", L"Nro Launcher", MB_OK | MB_ICONWARNING);
        return;
    }
    if (running[slot] != NULL)
    {
        if (WaitForSingleObject(running[slot], 0) == WAIT_TIMEOUT)
        {
            wchar_t message[80];
            _snwprintf(message, 80, L"Nro %d đang chạy.", slot);
            MessageBoxW(window, message, L"Nro Launcher", MB_OK | MB_ICONINFORMATION);
            return;
        }
        CloseHandle(running[slot]);
        running[slot] = NULL;
    }

    wchar_t command[MAX_PATH + 64];
    if (!NroBuildCommand(gameExe, slot, command, MAX_PATH + 64))
    {
        MessageBoxW(window, L"Không tạo được lệnh mở game.", L"Nro Launcher", MB_OK | MB_ICONERROR);
        return;
    }

    STARTUPINFOW startup = {0};
    PROCESS_INFORMATION process = {0};
    startup.cb = sizeof(startup);
    if (!CreateProcessW(gameExe, command, NULL, NULL, FALSE, 0, NULL, gameDir, &startup, &process))
    {
        wchar_t message[160];
        _snwprintf(message, 160, L"Không mở được Nro %d (lỗi Windows %lu).", slot, GetLastError());
        MessageBoxW(window, message, L"Nro Launcher", MB_OK | MB_ICONERROR);
        return;
    }
    CloseHandle(process.hThread);
    running[slot] = process.hProcess;
    RefreshRunning();
}

static LRESULT CALLBACK WindowProcedure(HWND window, UINT message, WPARAM wParam, LPARAM lParam)
{
    (void)lParam;
    switch (message)
    {
    case WM_CREATE:
        AddControl(window, L"STATIC", L"Mỗi số phiên lưu tài khoản và cài đặt riêng. Game dùng chung một thư mục.",
                   0, 15, 15, 360, 42, 0);
        AddControl(window, L"STATIC", L"Mở nhanh:", 0, 15, 67, 360, 20, 0);
        for (int slot = 1; slot <= 5; slot++)
        {
            wchar_t label[16];
            _snwprintf(label, 16, L"Nro %d", slot);
            AddControl(window, L"BUTTON", label, BS_PUSHBUTTON,
                       15 + (slot - 1) * 73, 92, 67, 38, 100 + slot);
        }
        AddControl(window, L"STATIC", L"Số phiên khác (1–999):", 0, 15, 151, 155, 25, 0);
        slotInput = AddControl(window, L"EDIT", L"6", WS_BORDER | ES_NUMBER | ES_AUTOHSCROLL,
                               172, 149, 72, 25, ID_SLOT_INPUT);
        AddControl(window, L"BUTTON", L"Mở phiên", BS_PUSHBUTTON, 256, 145, 118, 32, ID_OPEN_OTHER);
        statusLabel = AddControl(window, L"STATIC", L"Chọn một số phiên để mở game.",
                                 0, 15, 202, 360, 36, 0);
        SetTimer(window, TIMER_RUNNING, 2000, NULL);
        return 0;
    case WM_COMMAND:
    {
        int id = LOWORD(wParam);
        if (id >= 101 && id <= 105)
            OpenSlot(window, id - 100);
        else if (id == ID_OPEN_OTHER)
        {
            wchar_t value[32];
            GetWindowTextW(slotInput, value, 32);
            OpenSlot(window, NroParseSlot(value));
        }
        return 0;
    }
    case WM_TIMER:
        if (wParam == TIMER_RUNNING)
            RefreshRunning();
        return 0;
    case WM_CLOSE:
        DestroyWindow(window);
        return 0;
    case WM_DESTROY:
        KillTimer(window, TIMER_RUNNING);
        for (int slot = 1; slot <= 999; slot++)
        {
            if (running[slot] != NULL)
                CloseHandle(running[slot]);
        }
        PostQuitMessage(0);
        return 0;
    default:
        return DefWindowProcW(window, message, wParam, lParam);
    }
}

int WINAPI WinMain(HINSTANCE instance, HINSTANCE previous, LPSTR commandLine, int showCommand)
{
    (void)previous;
    (void)commandLine;
    wchar_t launcherPath[MAX_PATH];
    DWORD length = GetModuleFileNameW(NULL, launcherPath, MAX_PATH);
    if (length == 0 || length >= MAX_PATH)
        return 1;
    wchar_t *separator = wcsrchr(launcherPath, L'\\');
    if (separator == NULL)
        return 1;
    *separator = L'\0';
    if (!NroFindGame(launcherPath, gameDir, MAX_PATH, gameExe, MAX_PATH))
    {
        MessageBoxW(NULL, L"Không tìm thấy ProjCompleted.exe. Đặt launcher cùng thư mục với game hoặc trong thư mục con trực tiếp.",
                    L"Nro Launcher", MB_OK | MB_ICONWARNING);
        return 1;
    }

    WNDCLASSW windowClass = {0};
    windowClass.lpfnWndProc = WindowProcedure;
    windowClass.hInstance = instance;
    windowClass.lpszClassName = WINDOW_CLASS;
    windowClass.hCursor = LoadCursorW(NULL, IDC_ARROW);
    windowClass.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    if (!RegisterClassW(&windowClass))
        return 1;

    RECT frame = {0, 0, 390, 270};
    AdjustWindowRect(&frame, WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX, FALSE);
    int width = frame.right - frame.left;
    int height = frame.bottom - frame.top;
    int x = (GetSystemMetrics(SM_CXSCREEN) - width) / 2;
    int y = (GetSystemMetrics(SM_CYSCREEN) - height) / 2;
    HWND window = CreateWindowExW(0, WINDOW_CLASS, L"Nro Launcher",
                                  WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX,
                                  x, y, width, height, NULL, NULL, instance, NULL);
    if (window == NULL)
        return 1;
    ShowWindow(window, showCommand);
    UpdateWindow(window);

    MSG event;
    while (GetMessageW(&event, NULL, 0, 0) > 0)
    {
        TranslateMessage(&event);
        DispatchMessageW(&event);
    }
    return (int)event.wParam;
}
