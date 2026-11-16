$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$game1 = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root 'DecompiledSource\Game1\Panel.cs')
$game2 = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root 'DecompiledSource\Game2\Panel.cs')

function Get-MethodSource([string]$source, [string]$signature) {
    $start = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing method: $signature" }
    $open = $source.IndexOf('{', $start)
    $depth = 0
    for ($i = $open; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        elseif ($source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $source.Substring($start, $i - $start + 1) }
        }
    }
    throw "Unclosed method: $signature"
}

$signatures = @(
    'private string BossFavoriteKey(TopInfo boss)',
    'private bool IsBossDead(TopInfo boss)',
    'private bool IsBossAlive(TopInfo boss)',
    'private void BuildBossVisibleIndices()'
)
$methods = foreach ($signature in $signatures) {
    $left = Get-MethodSource $game1 $signature
    $right = Get-MethodSource $game2 $signature
    if ($left -cne $right) { throw "Game1/Game2 differ: $signature" }
    $left
}

foreach ($source in @($game1, $game2)) {
    $required = @(
        'bossVisibleIndices[selected]',
        'sendTop(topName, (sbyte)originalIndex)',
        'myVector.addElement(new Command(label, this, 1181296, boss))',
        'if (idAction == 1181296 && p is TopInfo bossFavorite',
        'ToggleBossFavorite(bossFavorite)',
        'RefreshBossVisibleIndices(true)',
        'RefreshBossVisibleIndices(false)',
        'Rms.saveRMSString("bossFavorites_"',
        'Rms.loadRMSString("bossFavorites_"',
        'selected < 0 || selected >= currentListLength'
    )
    foreach ($fragment in $required) {
        if (-not $source.Contains($fragment)) { throw "Missing flow: $fragment" }
    }
    if ($source.Contains('PaintBossStar(') -or $source.Contains('bossFavoriteTapPending')) {
        throw 'Old row-star interaction is still present'
    }
}

$harness = @'
using System;
using System.Collections.Generic;
public class TopInfo { public string name; public int rank; public string info; }
public class MyVector {
    public readonly List<TopInfo> rows = new List<TopInfo>();
    public int size() { return rows.Count; }
    public object elementAt(int index) { return rows[index]; }
}
public class BossPanelHarness {
    public readonly MyVector vTop = new MyVector();
    private readonly List<int> bossVisibleIndices = new List<int>();
    private readonly HashSet<string> bossFavorites = new HashSet<string>();
    private bool bossFavoritesOnly;
    private int bossLifeFilter;
    public void Add(string name, int rank, string info) { vTop.rows.Add(new TopInfo { name = name, rank = rank, info = info }); }
    public void Favorite(int index) { bossFavorites.Add(BossFavoriteKey(vTop.rows[index])); }
    public void ClearFavorites() { bossFavorites.Clear(); }
    public void Filter(bool favoritesOnly, int lifeFilter) { bossFavoritesOnly = favoritesOnly; bossLifeFilter = lifeFilter; BuildBossVisibleIndices(); }
    public string Visible() { return string.Join(",", bossVisibleIndices); }
    public int OriginalIndex(int visibleIndex) { return bossVisibleIndices[visibleIndex]; }
'@ + [Environment]::NewLine + ($methods -join [Environment]::NewLine) + [Environment]::NewLine + '}'

Add-Type -TypeDefinition $harness -ErrorAction Stop
$test = [BossPanelHarness]::new()
$test.Add('Số 17', -35, 'HP: 1,5 Tỷ')
$test.Add('Khi Đột', -219, 'Chết')
$test.Add('Whis', 550, 'HP: 3 Tỷ')
$test.Add('Số 17', -35, 'Chết')
$test.Add('Không rõ', 0, 'Đang chờ')
$test.Favorite(1)
$test.Favorite(2)

function Assert-Visible([BossPanelHarness]$panel, [bool]$favoriteOnly, [int]$life, [string]$expected) {
    $panel.Filter($favoriteOnly, $life)
    if ($panel.Visible() -cne $expected) { throw "Expected [$expected], got [$($panel.Visible())]" }
}

Assert-Visible $test $false 0 '1,2,0,3,4'
Assert-Visible $test $false 1 '2,0'
Assert-Visible $test $false 2 '1,3'
Assert-Visible $test $true 1 '2'
Assert-Visible $test $true 2 '1'
Assert-Visible $test $true 0 '1,2'
if ($test.OriginalIndex(1) -ne 2) { throw 'Filtered selection lost original server index' }
$test.ClearFavorites()
Assert-Visible $test $true 0 ''
Assert-Visible $test $false 0 '0,1,2,3,4'

Write-Output 'BossPanelTests PASS: 8 filter/selection scenarios including unknown status; Game1/Game2 flow and methods match.'
