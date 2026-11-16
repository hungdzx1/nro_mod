#define UNICODE
#define _UNICODE
#include <windows.h>
#include <wchar.h>
#include <stdio.h>
#include "../LauncherCore.h"

static int passed;
static int failed;

static void Check(int condition, const char *name)
{
    if (condition)
    {
        passed++;
        printf("PASS: %s\n", name);
    }
    else
    {
        failed++;
        printf("FAIL: %s\n", name);
    }
}

int main(void)
{
    wchar_t projectRoot[MAX_PATH];
    DWORD length = GetCurrentDirectoryW(MAX_PATH, projectRoot);
    if (length == 0 || length >= MAX_PATH)
        return 1;

    wchar_t myMod[MAX_PATH];
    wchar_t nested[MAX_PATH];
    wchar_t gameDir[MAX_PATH];
    wchar_t gameExe[MAX_PATH];
    wchar_t command[MAX_PATH + 64];
    _snwprintf(myMod, MAX_PATH, L"%ls\\MyMod", projectRoot);
    _snwprintf(nested, MAX_PATH, L"%ls\\NroLauncherExe\\Tests", myMod);

    Check(NroFindGame(projectRoot, gameDir, MAX_PATH, gameExe, MAX_PATH) &&
          wcscmp(gameDir, projectRoot) == 0, "game beside launcher");
    Check(NroFindGame(myMod, gameDir, MAX_PATH, gameExe, MAX_PATH) &&
          wcscmp(gameDir, projectRoot) == 0, "game in direct parent");
    Check(!NroFindGame(nested, gameDir, MAX_PATH, gameExe, MAX_PATH), "no deeper search");
    Check(NroIsValidSlot(1) && NroIsValidSlot(999) && !NroIsValidSlot(0) &&
          !NroIsValidSlot(1000), "slot limits");
    Check(NroParseSlot(L"1") == 1 && NroParseSlot(L"999") == 999,
          "parse valid slots");
    Check(NroParseSlot(L"0") == 0 && NroParseSlot(L"1000") == 0 &&
          NroParseSlot(L"abc") == 0 && NroParseSlot(L"1x") == 0,
          "reject invalid slots");
    Check(NroBuildCommand(L"C:\\Game Folder\\ProjCompleted.exe", 2, command,
                          MAX_PATH + 64) &&
          wcscmp(command, L"\"C:\\Game Folder\\ProjCompleted.exe\" --nro-slot=2") == 0,
          "quoted game path and slot argument");
    Check(!NroBuildCommand(L"game.exe", 0, command, MAX_PATH + 64),
          "reject invalid launch slot");
    Check(!NroBuildCommand(L"game.exe", 2, command, 5),
          "reject truncated command");
    printf("%d/9 PASS\n", passed);
    return failed == 0 ? 0 : 1;
}
