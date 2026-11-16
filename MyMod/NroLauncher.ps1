Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$gameDir = $PSScriptRoot
$gameExe = Join-Path $gameDir 'ProjCompleted.exe'
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
    $gameDir = Split-Path -Parent $PSScriptRoot
    $gameExe = Join-Path $gameDir 'ProjCompleted.exe'
}
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
    [System.Windows.Forms.MessageBox]::Show('Khong tim thay ProjCompleted.exe. Dat launcher cung thu muc voi game.', 'Nro Launcher') | Out-Null
    exit 1
}

$running = @{}
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Nro Launcher'
$form.StartPosition = 'CenterScreen'
$form.ClientSize = New-Object System.Drawing.Size(390, 270)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

$intro = New-Object System.Windows.Forms.Label
$intro.Location = New-Object System.Drawing.Point(15, 15)
$intro.Size = New-Object System.Drawing.Size(360, 42)
$intro.Text = 'Moi so phien luu acc va cai dat rieng. Game van dung chung mot thu muc.'
$form.Controls.Add($intro)

$quickLabel = New-Object System.Windows.Forms.Label
$quickLabel.Location = New-Object System.Drawing.Point(15, 67)
$quickLabel.Size = New-Object System.Drawing.Size(360, 20)
$quickLabel.Text = 'Mo nhanh:'
$form.Controls.Add($quickLabel)

function Open-NroSlot([int]$slot) {
    if ($running.ContainsKey($slot)) {
        try {
            if (-not $running[$slot].HasExited) {
                [System.Windows.Forms.MessageBox]::Show("Nro $slot dang chay.", 'Nro Launcher') | Out-Null
                return
            }
        } catch { }
        $running.Remove($slot)
    }
    try {
        $process = Start-Process -FilePath $gameExe -WorkingDirectory $gameDir -ArgumentList "--nro-slot=$slot" -PassThru
        $running[$slot] = $process
        $status.Text = "Da mo Nro $slot. Dang chay: $($running.Count) phien."
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Khong mo duoc Nro $slot`: $($_.Exception.Message)", 'Nro Launcher') | Out-Null
    }
}

for ($slotNumber = 1; $slotNumber -le 5; $slotNumber++) {
    $button = New-Object System.Windows.Forms.Button
    $button.Location = New-Object System.Drawing.Point((15 + ($slotNumber - 1) * 73), 92)
    $button.Size = New-Object System.Drawing.Size(67, 38)
    $button.Text = "Nro $slotNumber"
    $button.Tag = $slotNumber
    $button.Add_Click({ Open-NroSlot ([int]$this.Tag) })
    $form.Controls.Add($button)
}

$moreLabel = New-Object System.Windows.Forms.Label
$moreLabel.Location = New-Object System.Drawing.Point(15, 151)
$moreLabel.Size = New-Object System.Drawing.Size(155, 25)
$moreLabel.Text = 'So phien khac (1-999):'
$form.Controls.Add($moreLabel)

$slotPicker = New-Object System.Windows.Forms.NumericUpDown
$slotPicker.Location = New-Object System.Drawing.Point(172, 149)
$slotPicker.Size = New-Object System.Drawing.Size(72, 25)
$slotPicker.Minimum = 1
$slotPicker.Maximum = 999
$slotPicker.Value = 6
$form.Controls.Add($slotPicker)

$openButton = New-Object System.Windows.Forms.Button
$openButton.Location = New-Object System.Drawing.Point(256, 145)
$openButton.Size = New-Object System.Drawing.Size(118, 32)
$openButton.Text = 'Mo phien'
$openButton.Add_Click({ Open-NroSlot ([int]$slotPicker.Value) })
$form.Controls.Add($openButton)

$status = New-Object System.Windows.Forms.Label
$status.Location = New-Object System.Drawing.Point(15, 202)
$status.Size = New-Object System.Drawing.Size(360, 36)
$status.Text = 'Chon mot so phien de mo game.'
$form.Controls.Add($status)

$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 2000
$timer.Add_Tick({
    foreach ($slot in @($running.Keys)) {
        try {
            if ($running[$slot].HasExited) { $running.Remove($slot) }
        } catch { $running.Remove($slot) }
    }
    $status.Text = "Dang chay tu launcher nay: $($running.Count) phien."
})
$timer.Start()
$form.Add_FormClosed({ $timer.Stop(); $timer.Dispose() })
[void]$form.ShowDialog()
