#define UNICODE
#define _UNICODE
#include <windows.h>
#include <stdio.h>
#include <wctype.h>
#include "LauncherCore.h"

int NroIsValidSlot(int slot)
{
    return slot >= 1 && slot <= 999;
}

int NroParseSlot(const wchar_t *text)
{
    if (text == NULL || !iswdigit(*text))
        return 0;

    wchar_t *end = NULL;
    long value = wcstol(text, &end, 10);
    while (iswspace(*end))
        end++;
    return *end == L'\0' && NroIsValidSlot((int)value) && value == (long)(int)value
        ? (int)value : 0;
}

int NroBuildCommand(const wchar_t *gameExe, int slot, wchar_t *output, size_t capacity)
{
    if (gameExe == NULL || output == NULL || capacity == 0 || !NroIsValidSlot(slot))
        return 0;
    int written = _snwprintf(output, capacity, L"\"%ls\" --nro-slot=%d", gameExe, slot);
    return written >= 0 && (size_t)written < capacity;
}

static int CopyWide(wchar_t *output, size_t capacity, const wchar_t *input)
{
    size_t length = wcslen(input);
    if (output == NULL || length >= capacity)
        return 0;
    wcscpy(output, input);
    return 1;
}

static int GameInDirectory(const wchar_t *directory, wchar_t *gameExe, size_t exeCapacity)
{
    const wchar_t *fileName = L"ProjCompleted.exe";
    size_t length = wcslen(directory);
    const wchar_t *separator = length > 0 && (directory[length - 1] == L'\\' || directory[length - 1] == L'/')
        ? L"" : L"\\";
    int written = _snwprintf(gameExe, exeCapacity, L"%ls%ls%ls", directory, separator, fileName);
    if (written < 0 || (size_t)written >= exeCapacity)
        return 0;
    DWORD attributes = GetFileAttributesW(gameExe);
    return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) == 0;
}

int NroFindGame(const wchar_t *launcherDir, wchar_t *gameDir, size_t dirCapacity,
                wchar_t *gameExe, size_t exeCapacity)
{
    if (launcherDir == NULL || gameDir == NULL || gameExe == NULL ||
        !CopyWide(gameDir, dirCapacity, launcherDir))
        return 0;

    if (GameInDirectory(gameDir, gameExe, exeCapacity))
        return 1;

    size_t length = wcslen(gameDir);
    while (length > 0 && (gameDir[length - 1] == L'\\' || gameDir[length - 1] == L'/'))
        gameDir[--length] = L'\0';
    wchar_t *backslash = wcsrchr(gameDir, L'\\');
    wchar_t *slash = wcsrchr(gameDir, L'/');
    wchar_t *last = backslash;
    if (slash != NULL && (last == NULL || slash > last))
        last = slash;
    if (last == NULL || last == gameDir)
        return 0;
    if (last == gameDir + 2 && gameDir[1] == L':')
        last[1] = L'\0';
    else
        *last = L'\0';
    return GameInDirectory(gameDir, gameExe, exeCapacity);
}
