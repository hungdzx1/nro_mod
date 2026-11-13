$rootDir = Split-Path -Parent $PSScriptRoot
$backupDir = Join-Path $rootDir 'Backup'

Write-Host "Đang sao lưu Managed DLL..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path "$backupDir\Managed" -Force | Out-Null
Copy-Item "$rootDir\ProjCompleted_Data\Managed\Assembly-CSharp.dll" "$backupDir\Managed\Assembly-CSharp.dll" -Force

Write-Host "Đang sao lưu DecompiledSource..." -ForegroundColor Cyan
$files = @(
    'Game1\ModFunc.cs',
    'Game2\ModFunc.cs',
    'Game1\Panel.cs',
    'Game2\Panel.cs',
    'Game1\Controller.cs',
    'Game2\Controller.cs',
    'Game1\GameScr.cs',
    'Game2\GameScr.cs',
    'Game1\InfoMe.cs',
    'Game2\InfoMe.cs',
    'Game1.Assets.src.f\Controller2.cs',
    'Game2.Assets.src.f\Controller2.cs',
    'Game1.Mod\AutoGop.cs',
    'Game2.Mod\AutoGop.cs',
    'Game1.Mod\AutoGiaoDich.cs',
    'Game2.Mod\AutoGiaoDich.cs',
    'Game1.Mod\AutoDungNhanh.cs',
    'Game2.Mod\AutoDungNhanh.cs',
    'Game1.Mod\AutoMuaNhieu.cs',
    'Game2.Mod\AutoMuaNhieu.cs',
    'Game1.Mod\NpcMenuHelper.cs',
    'Game2.Mod\NpcMenuHelper.cs',
    'Game1\ServerListScreen.cs',
    'Game2\ServerListScreen.cs',
    'Assembly-CSharp.csproj'
)

foreach ($f in $files) {
    $src = Join-Path "$rootDir\DecompiledSource" $f
    $dst = Join-Path "$backupDir\DecompiledSource" $f
    $parent = Split-Path $dst -Parent
    if (!(Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    Copy-Item $src $dst -Force
}

Write-Host "Đang sao lưu MyMod..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path "$backupDir\MyMod" -Force | Out-Null
Copy-Item -Recurse "$rootDir\MyMod\*" "$backupDir\MyMod\" -Force

Write-Host "Đang sao lưu Kehoach..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path "$backupDir\Kehoach" -Force | Out-Null
Copy-Item -Recurse "$rootDir\Kehoach\*" "$backupDir\Kehoach\" -Force

# Tạo bản nén Zip có gắn timestamp để lưu trữ an toàn
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipPath = "$backupDir\Backup_Snapshot_$timestamp.zip"
Write-Host "Đang tạo bản nén đóng gói: $zipPath ..." -ForegroundColor Cyan

Compress-Archive -Path "$backupDir\Managed", "$backupDir\DecompiledSource", "$backupDir\MyMod", "$backupDir\Kehoach" -DestinationPath $zipPath -Force

Write-Host "Sao lưu toàn bộ Data và Mod thành công!" -ForegroundColor Green
