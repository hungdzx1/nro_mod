$ErrorActionPreference = 'Stop'
$sourceDir = $PSScriptRoot
$projectRoot = Split-Path -Parent (Split-Path -Parent $sourceDir)
$testExe = Join-Path $sourceDir 'Tests\LauncherCoreTests.exe'
$launcherExe = Join-Path (Split-Path -Parent $sourceDir) 'NroLauncher.exe'
$commonFlags = @('-std=c11', '-Wall', '-Wextra', '-Werror', '-finput-charset=UTF-8')

try {
    & gcc @commonFlags (Join-Path $sourceDir 'Tests\LauncherCoreTests.c') (Join-Path $sourceDir 'LauncherCore.c') -o $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Launcher test compilation failed.' }

    Push-Location $projectRoot
    try {
        & $testExe
        if ($LASTEXITCODE -ne 0) { throw 'Launcher tests failed.' }
    }
    finally {
        Pop-Location
    }

    & gcc @commonFlags -mwindows -static-libgcc (Join-Path $sourceDir 'NroLauncher.c') (Join-Path $sourceDir 'LauncherCore.c') -o $launcherExe -lgdi32 -luser32
    if ($LASTEXITCODE -ne 0) { throw 'NroLauncher.exe compilation failed.' }

    Write-Host "Created $launcherExe"
}
finally {
    if (Test-Path -LiteralPath $testExe -PathType Leaf) {
        Remove-Item -LiteralPath $testExe
    }
}
