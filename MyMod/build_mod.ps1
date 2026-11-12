# Script tự động biên dịch và cập nhật Assembly-CSharp.dll vào Game
$dotnetExe = "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Data\DotNetSdk\dotnet.exe"
$rootDir = Split-Path -Parent $PSScriptRoot

Write-Host "Đang biên dịch Assembly-CSharp.dll..." -ForegroundColor Cyan
& $dotnetExe build "$rootDir\DecompiledSource\Assembly-CSharp.csproj" -c Release -v minimal

if ($LASTEXITCODE -eq 0) {
    Copy-Item "$rootDir\DecompiledSource\bin\Release\netstandard2.1\Assembly-CSharp.dll" "$rootDir\ProjCompleted_Data\Managed\Assembly-CSharp.dll" -Force
    Write-Host "Đã cập nhật thành công ProjCompleted_Data\Managed\Assembly-CSharp.dll!" -ForegroundColor Green
} else {
    Write-Host "Biên dịch thất bại!" -ForegroundColor Red
}

