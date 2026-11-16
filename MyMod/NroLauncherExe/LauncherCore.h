#ifndef NRO_LAUNCHER_CORE_H
#define NRO_LAUNCHER_CORE_H

#include <stddef.h>
#include <wchar.h>

int NroIsValidSlot(int slot);
int NroParseSlot(const wchar_t *text);
int NroBuildCommand(const wchar_t *gameExe, int slot, wchar_t *output, size_t capacity);
int NroFindGame(const wchar_t *launcherDir, wchar_t *gameDir, size_t dirCapacity,
                wchar_t *gameExe, size_t exeCapacity);

#endif
