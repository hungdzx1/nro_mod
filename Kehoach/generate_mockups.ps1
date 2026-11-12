Add-Type -AssemblyName System.Drawing

function Create-Mockup1 {
    param([string]$path)
    $w = 860; $h = 540
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    # Background (Retro sky & ground)
    $skyBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0,0), (New-Object System.Drawing.Point 0,$h), [System.Drawing.Color]::FromArgb(255, 60, 140, 220), [System.Drawing.Color]::FromArgb(255, 140, 200, 250)
    $g.FillRectangle($skyBrush, 0, 0, $w, 360)
    $skyBrush.Dispose()
    
    $groundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0,360), (New-Object System.Drawing.Point 0,$h), [System.Drawing.Color]::FromArgb(255, 80, 160, 60), [System.Drawing.Color]::FromArgb(255, 40, 100, 30)
    $g.FillRectangle($groundBrush, 0, 360, $w, 180)
    $groundBrush.Dispose()

    # Fonts
    $fontTitle = New-Object System.Drawing.Font "Tahoma", 11, [System.Drawing.FontStyle]::Bold
    $fontBold = New-Object System.Drawing.Font "Tahoma", 9.5, [System.Drawing.FontStyle]::Bold
    $fontRegular = New-Object System.Drawing.Font "Tahoma", 9, [System.Drawing.FontStyle]::Regular
    $fontSmall = New-Object System.Drawing.Font "Tahoma", 8, [System.Drawing.FontStyle]::Regular
    $fontSmallBold = New-Object System.Drawing.Font "Tahoma", 8, [System.Drawing.FontStyle]::Bold

    # Top HUD Bar
    $hudBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 0, 0, 0))
    $g.FillRectangle($hudBg, 15, 12, 380, 52)
    $hudBg.Dispose()
    $g.DrawRectangle([System.Drawing.Pens]::DimGray, 15, 12, 380, 52)

    # HP / MP Bar
    $g.FillRectangle([System.Drawing.Brushes]::Red, 25, 20, 140, 14)
    $g.DrawRectangle([System.Drawing.Pens]::Black, 25, 20, 140, 14)
    $g.DrawString("HP: 125,450 / 125,450", $fontSmallBold, [System.Drawing.Brushes]::White, 28, 20)

    $g.FillRectangle([System.Drawing.Brushes]::DodgerBlue, 25, 40, 140, 14)
    $g.DrawRectangle([System.Drawing.Pens]::Black, 25, 40, 140, 14)
    $g.DrawString("KI: 80,200 / 80,200", $fontSmallBold, [System.Drawing.Brushes]::White, 28, 40)

    $g.DrawString("Vang: 2.450.000.000", $fontSmallBold, [System.Drawing.Brushes]::Yellow, 185, 20)
    $g.DrawString("Ngoc: 1.520", $fontSmallBold, [System.Drawing.Brushes]::LightGreen, 185, 40)
    $g.DrawString("Map: Dao Kame - Khu 12", $fontSmallBold, [System.Drawing.Brushes]::White, 275, 40)

    # Main Shop Panel
    $px = 80; $py = 75; $pw = 700; $ph = 430
    $panelBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 40, 42, 48))
    $g.FillRectangle($panelBg, $px, $py, $pw, $ph)
    $panelBg.Dispose()
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 165, 32), 2)), $px, $py, $pw, $ph)

    # Shop Header Tabs
    $tabBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 70, 75, 85))
    $g.FillRectangle($tabBrush, $px + 10, $py + 10, 160, 32)
    $g.DrawRectangle([System.Drawing.Pens]::Goldenrod, $px + 10, $py + 10, 160, 32)
    $g.DrawString("CỬA HÀNG BÙA / ĐỒ", $fontBold, [System.Drawing.Brushes]::Gold, $px + 20, $py + 17)

    $g.FillRectangle($tabBrush, $px + 180, $py + 10, 140, 32)
    $g.DrawRectangle([System.Drawing.Pens]::Gray, $px + 180, $py + 10, 140, 32)
    $g.DrawString("HÀNH TRANG", $fontBold, [System.Drawing.Brushes]::LightGray, $px + 205, $py + 17)
    $tabBrush.Dispose()

    # Left Column: Item List
    $lx = $px + 15; $ly = $py + 55; $lw = 320; $lh = 360
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 25, 27, 30))), $lx, $ly, $lw, $lh)
    $g.DrawRectangle([System.Drawing.Pens]::DarkGray, $lx, $ly, $lw, $lh)

    $items = @(
        @{Name="Thỏi Vàng"; Desc="Quy đổi 500tr vàng"; IconColor=[System.Drawing.Color]::Gold; Price="500Tr Vàng"; Sel=$false},
        @{Name="Cuồng Nộ"; Desc="Tăng 100% Sức đánh trong 10p"; IconColor=[System.Drawing.Color]::Crimson; Price="5 Ngọc"; Sel=$true},
        @{Name="Bổ Huyết"; Desc="Tăng 100% HP trong 10p"; IconColor=[System.Drawing.Color]::DarkRed; Price="5 Ngọc"; Sel=$false},
        @{Name="Bổ Khí"; Desc="Tăng 100% KI trong 10p"; IconColor=[System.Drawing.Color]::RoyalBlue; Price="5 Ngọc"; Sel=$false},
        @{Name="Giáp Xên"; Desc="Giảm 50% sát thương nhận"; IconColor=[System.Drawing.Color]::LimeGreen; Price="5 Ngọc"; Sel=$false}
    )

    $ity = $ly + 8
    foreach ($it in $items) {
        if ($it.Sel) {
            $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 90, 70, 20))), $lx + 6, $ity, $lw - 12, 60)
            $g.DrawRectangle([System.Drawing.Pens]::Gold, $lx + 6, $ity, $lw - 12, 60)
        } else {
            $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 45, 48, 55))), $lx + 6, $ity, $lw - 12, 60)
            $g.DrawRectangle([System.Drawing.Pens]::DimGray, $lx + 6, $ity, $lw - 12, 60)
        }

        # Item Icon Box
        $g.FillRectangle((New-Object System.Drawing.SolidBrush $it.IconColor), $lx + 15, $ity + 10, 40, 40)
        $g.DrawRectangle([System.Drawing.Pens]::White, $lx + 15, $ity + 10, 40, 40)
        $g.DrawString("★", $fontTitle, [System.Drawing.Brushes]::White, $lx + 24, $ity + 18)

        # Item Text
        $g.DrawString($it.Name, $fontBold, [System.Drawing.Brushes]::White, $lx + 65, $ity + 10)
        $g.DrawString($it.Desc, $fontSmall, [System.Drawing.Brushes]::LightGray, $lx + 65, $ity + 28)
        $g.DrawString("Giá: " + $it.Price, $fontSmallBold, [System.Drawing.Brushes]::Yellow, $lx + 65, $ity + 42)

        $ity += 68
    }

    # Right Column: Detail & Action Buttons
    $rx = $px + 345; $ry = $py + 55; $rw = 340; $rh = 360
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 25, 27, 30))), $rx, $ry, $rw, $rh)
    $g.DrawRectangle([System.Drawing.Pens]::DarkGray, $rx, $ry, $rw, $rh)

    # Selected Item Detail Card
    $g.DrawString("CHI TIẾT VẬT PHẨM ĐANG CHỌN", $fontBold, [System.Drawing.Brushes]::Orange, $rx + 15, $ry + 15)
    $g.DrawLine([System.Drawing.Pens]::Gray, $rx + 15, $ry + 35, $rx + $rw - 15, $ry + 35)

    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Crimson)), $rx + 20, $ry + 45, 48, 48)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, $rx + 20, $ry + 45, 48, 48)
    $g.DrawString("CN", $fontTitle, [System.Drawing.Brushes]::White, $rx + 28, $ry + 58)

    $g.DrawString("Cuồng Nộ (Cấp 2)", $fontTitle, [System.Drawing.Brushes]::Yellow, $rx + 80, $ry + 48)
    $g.DrawString("Tác dụng: Tăng gấp đôi sức đánh trong 10 phút.", $fontSmall, [System.Drawing.Brushes]::LightGreen, $rx + 80, $ry + 70)
    $g.DrawString("Đơn giá: 5 Ngọc / cái", $fontSmallBold, [System.Drawing.Brushes]::White, $rx + 80, $ry + 85)

    # Standard Buttons
    $btnY = $ry + 115
    $stdBtns = @("Mua x1 (5 Ngọc)", "Mua 20 lần", "Mua 50 lần", "Mua 100 lần")
    foreach ($sb in $stdBtns) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 60, 65, 75))), $rx + 20, $btnY, $rw - 40, 26)
        $g.DrawRectangle([System.Drawing.Pens]::Silver, $rx + 20, $btnY, $rw - 40, 26)
        $g.DrawString($sb, $fontRegular, [System.Drawing.Brushes]::White, $rx + 110, $btnY + 5)
        $btnY += 32
    }

    # HIGHLIGHT NEW BUTTON (MẪU 1)
    $btnY += 8
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 180, 70, 0))), $rx + 15, $btnY, $rw - 30, 48)
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::Gold, 2.5)), $rx + 15, $btnY, $rw - 30, 48)
    
    $g.DrawString("⚡ AUTO MUA NHANH (1.000 / LẦN)", $fontTitle, [System.Drawing.Brushes]::White, $rx + 35, $btnY + 8)
    $g.DrawString("Chế độ ngầm: Bắn thẳng 1.000 lên Server - Ẩn ô nhập", $fontSmallBold, [System.Drawing.Brushes]::Yellow, $rx + 25, $btnY + 28)

    # Banner Label at bottom of dialog
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 10, 40, 10))), $rx + 15, $btnY + 58, $rw - 30, 30)
    $g.DrawRectangle([System.Drawing.Pens]::GreenYellow, $rx + 15, $btnY + 58, $rw - 30, 30)
    $g.DrawString("✔ Phím nóng: Bấm Q bất kỳ lúc nào để DỪNG NGAY", $fontSmallBold, [System.Drawing.Brushes]::Lime, $rx + 25, $btnY + 66)

    # Watermark Mẫu 1
    $tagBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 20, 20, 20))
    $g.FillRectangle($tagBrush, 15, $h - 38, 380, 28)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, 15, $h - 38, 380, 28)
    $g.DrawString("MẪU 1: TÍCH HỢP NÚT VÀO MENU SHOP", $fontBold, [System.Drawing.Brushes]::Yellow, 25, $h - 32)
    $tagBrush.Dispose()

    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $g.Dispose()
}

function Create-Mockup2 {
    param([string]$path)
    $w = 860; $h = 540
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    # Background (Game world dim background)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 30, 38, 48))
    
    # Fonts
    $fontTitle = New-Object System.Drawing.Font "Tahoma", 12, [System.Drawing.FontStyle]::Bold
    $fontBold = New-Object System.Drawing.Font "Tahoma", 10, [System.Drawing.FontStyle]::Bold
    $fontRegular = New-Object System.Drawing.Font "Tahoma", 9, [System.Drawing.FontStyle]::Regular
    $fontSmall = New-Object System.Drawing.Font "Tahoma", 8.5, [System.Drawing.FontStyle]::Regular
    $fontSmallBold = New-Object System.Drawing.Font "Tahoma", 8.5, [System.Drawing.FontStyle]::Bold

    # Floating Dialog Box
    $dx = 150; $dy = 45; $dw = 560; $dh = 450
    $dialogBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 18, 20, 24))
    $g.FillRectangle($dialogBg, $dx, $dy, $dw, $dh)
    $dialogBg.Dispose()
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 230, 180, 40), 2.5)), $dx, $dy, $dw, $dh)

    # Header
    $headerBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 40, 44, 52))
    $g.FillRectangle($headerBg, $dx + 2, $dy + 2, $dw - 4, 40)
    $headerBg.Dispose()
    $g.DrawLine([System.Drawing.Pens]::Goldenrod, $dx, $dy + 42, $dx + $dw, $dy + 42)
    $g.DrawString("⚡ BẢNG CÀI ĐẶT AUTO MUA SỐ LƯỢNG LỚN", $fontTitle, [System.Drawing.Brushes]::Gold, $dx + 20, $dy + 12)
    
    # Close Button
    $g.FillRectangle([System.Drawing.Brushes]::Crimson, $dx + $dw - 35, $dy + 8, 26, 26)
    $g.DrawRectangle([System.Drawing.Pens]::White, $dx + $dw - 35, $dy + 8, 26, 26)
    $g.DrawString("X", $fontBold, [System.Drawing.Brushes]::White, $dx + $dw - 28, $dy + 12)

    # Content Rows
    $cy = $dy + 60

    # Row 1: Target Item Card
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 30, 34, 40))), $dx + 25, $cy, $dw - 50, 60)
    $g.DrawRectangle([System.Drawing.Pens]::DimGray, $dx + 25, $cy, $dw - 50, 60)
    
    $g.FillRectangle([System.Drawing.Brushes]::Goldenrod, $dx + 35, $cy + 10, 40, 40)
    $g.DrawRectangle([System.Drawing.Pens]::White, $dx + 35, $cy + 10, 40, 40)
    $g.DrawString("TV", $fontBold, [System.Drawing.Brushes]::Black, $dx + 44, $cy + 22)

    $g.DrawString("Vật phẩm mục tiêu:", $fontSmall, [System.Drawing.Brushes]::LightGray, $dx + 85, $cy + 12)
    $g.DrawString("Thỏi Vàng (ID: 457) - Giá: 500.000.000 Vàng", $fontBold, [System.Drawing.Brushes]::Yellow, $dx + 85, $cy + 28)
    
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 60, 90, 150))), $dx + $dw - 165, $cy + 15, 125, 30)
    $g.DrawRectangle([System.Drawing.Pens]::LightSkyBlue, $dx + $dw - 165, $cy + 15, 125, 30)
    $g.DrawString("🎯 Đổi từ Shop", $fontSmallBold, [System.Drawing.Brushes]::White, $dx + $dw - 150, $cy + 22)

    # Row 2: Batch Size
    $cy += 75
    $g.DrawString("Số lượng mỗi đợt:", $fontBold, [System.Drawing.Brushes]::White, $dx + 30, $cy + 5)
    $g.FillRectangle([System.Drawing.Brushes]::Black, $dx + 230, $cy, 120, 28)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, $dx + 230, $cy, 120, 28)
    $g.DrawString("1000", $fontBold, [System.Drawing.Brushes]::Lime, $dx + 270, $cy + 5)
    $g.DrawString("(Tối đa 1.000 / đợt theo Server)", $fontSmall, [System.Drawing.Brushes]::LightGray, $dx + 365, $cy + 7)

    # Row 3: Total Batches
    $cy += 40
    $g.DrawString("Số đợt cần mua:", $fontBold, [System.Drawing.Brushes]::White, $dx + 30, $cy + 5)
    $g.FillRectangle([System.Drawing.Brushes]::Black, $dx + 230, $cy, 120, 28)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, $dx + 230, $cy, 120, 28)
    $g.DrawString("5", $fontBold, [System.Drawing.Brushes]::Lime, $dx + 282, $cy + 5)
    $g.DrawString("=> Tổng mua: 5.000 cái (0 = mua vô hạn)", $fontSmallBold, [System.Drawing.Brushes]::Orange, $dx + 365, $cy + 7)

    # Row 4: Delay
    $cy += 40
    $g.DrawString("Độ trễ mỗi đợt (Delay):", $fontBold, [System.Drawing.Brushes]::White, $dx + 30, $cy + 5)
    $g.FillRectangle([System.Drawing.Brushes]::Black, $dx + 230, $cy, 120, 28)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, $dx + 230, $cy, 120, 28)
    $g.DrawString("150 ms", $fontBold, [System.Drawing.Brushes]::White, $dx + 265, $cy + 5)
    $g.DrawString("(Khuyên dùng: 100ms - 250ms chống văng)", $fontSmall, [System.Drawing.Brushes]::LightGray, $dx + 365, $cy + 7)

    # Row 5: Options Checkbox
    $cy += 42
    $g.DrawString("[✔] Tự động bắn thẳng 1.000 - Ẩn hoàn toàn bảng nhập ClientInput (Phương án 1)", $fontSmallBold, [System.Drawing.Brushes]::LightGreen, $dx + 30, $cy)
    $g.DrawString("[✔] Tự động dừng an toàn khi đầy hành trang hoặc hết vàng / ngọc", $fontSmallBold, [System.Drawing.Brushes]::LightGreen, $dx + 30, $cy + 20)

    # Row 6: Live Progress Bar
    $cy += 48
    $g.DrawString("Tiến độ:", $fontBold, [System.Drawing.Brushes]::White, $dx + 30, $cy + 5)
    $g.FillRectangle([System.Drawing.Brushes]::Black, $dx + 100, $cy + 3, $dw - 130, 24)
    $g.DrawRectangle([System.Drawing.Pens]::Gray, $dx + 100, $cy + 3, $dw - 130, 24)
    
    # 60% Fill
    $g.FillRectangle([System.Drawing.Brushes]::DodgerBlue, $dx + 102, $cy + 5, ($dw - 134) * 0.6, 20)
    $g.DrawString("Đã mua: 3.000 / 5.000 (60%)  -  Tốc độ: ~4.500 cái/giây", $fontSmallBold, [System.Drawing.Brushes]::White, $dx + 180, $cy + 7)

    # Row 7: Action Buttons
    $cy += 42
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 140, 40))), $dx + 35, $cy, 230, 42)
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::Lime, 2)), $dx + 35, $cy, 230, 42)
    $g.DrawString("▶ BẮT ĐẦU CHẠY", $fontBold, [System.Drawing.Brushes]::White, $dx + 80, $cy + 12)

    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 170, 30, 30))), $dx + 295, $cy, 230, 42)
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::Crimson, 2)), $dx + 295, $cy, 230, 42)
    $g.DrawString("⏹ DỪNG LẠI (Phím Q)", $fontBold, [System.Drawing.Brushes]::White, $dx + 335, $cy + 12)

    # Watermark Mẫu 2
    $tagBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 20, 20, 20))
    $g.FillRectangle($tagBrush, 15, $h - 38, 420, 28)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, 15, $h - 38, 420, 28)
    $g.DrawString("MẪU 2: CỬA SỔ NỔI CÀI ĐẶT RIÊNG (MOD WINDOW)", $fontBold, [System.Drawing.Brushes]::Yellow, 25, $h - 32)
    $tagBrush.Dispose()

    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $g.Dispose()
}

function Create-Mockup3 {
    param([string]$path)
    $w = 860; $h = 540
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    # Realistic In-Game Screen
    $skyBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0,0), (New-Object System.Drawing.Point 0,$h), [System.Drawing.Color]::FromArgb(255, 75, 160, 235), [System.Drawing.Color]::FromArgb(255, 160, 220, 255)
    $g.FillRectangle($skyBrush, 0, 0, $w, 380)
    $skyBrush.Dispose()

    $groundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0,380), (New-Object System.Drawing.Point 0,$h), [System.Drawing.Color]::FromArgb(255, 95, 180, 75), [System.Drawing.Color]::FromArgb(255, 50, 120, 40)
    $g.FillRectangle($groundBrush, 0, 380, $w, 160)
    $groundBrush.Dispose()

    # Fonts
    $fontTitle = New-Object System.Drawing.Font "Tahoma", 11, [System.Drawing.FontStyle]::Bold
    $fontBold = New-Object System.Drawing.Font "Tahoma", 9.5, [System.Drawing.FontStyle]::Bold
    $fontRegular = New-Object System.Drawing.Font "Tahoma", 9, [System.Drawing.FontStyle]::Regular
    $fontSmall = New-Object System.Drawing.Font "Tahoma", 8, [System.Drawing.FontStyle]::Regular
    $fontSmallBold = New-Object System.Drawing.Font "Tahoma", 8, [System.Drawing.FontStyle]::Bold

    # Character Silhouette & Name
    $g.FillEllipse([System.Drawing.Brushes]::Black, 400, 345, 50, 16)
    $g.FillRectangle([System.Drawing.Brushes]::DarkOrange, 410, 260, 30, 70)
    $g.FillEllipse([System.Drawing.Brushes]::PeachPuff, 412, 230, 26, 28)
    $g.DrawString("GokuSuper", $fontSmallBold, [System.Drawing.Brushes]::Yellow, 402, 212)
    $g.DrawString("[125.4k/125.4k]", $fontSmall, [System.Drawing.Brushes]::White, 395, 225)

    # Top HUD Bar
    $hudBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 0, 0, 0))
    $g.FillRectangle($hudBg, 15, 12, 380, 52)
    $hudBg.Dispose()
    $g.DrawRectangle([System.Drawing.Pens]::DimGray, 15, 12, 380, 52)

    $g.FillRectangle([System.Drawing.Brushes]::Red, 25, 20, 140, 14)
    $g.DrawRectangle([System.Drawing.Pens]::Black, 25, 20, 140, 14)
    $g.DrawString("HP: 125,450 / 125,450", $fontSmallBold, [System.Drawing.Brushes]::White, 28, 20)

    $g.FillRectangle([System.Drawing.Brushes]::DodgerBlue, 25, 40, 140, 14)
    $g.DrawRectangle([System.Drawing.Pens]::Black, 25, 40, 140, 14)
    $g.DrawString("KI: 80,200 / 80,200", $fontSmallBold, [System.Drawing.Brushes]::White, 28, 40)

    $g.DrawString("Vang: 2.450.000.000", $fontSmallBold, [System.Drawing.Brushes]::Yellow, 185, 20)
    $g.DrawString("Ngoc: 1.520", $fontSmallBold, [System.Drawing.Brushes]::LightGreen, 185, 40)
    $g.DrawString("Dao Kame - K12", $fontSmallBold, [System.Drawing.Brushes]::White, 275, 40)

    # Top Floating Live Status Banner
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 0, 0, 0))), 240, 75, 420, 32)
    $g.DrawRectangle([System.Drawing.Pens]::Goldenrod, 240, 75, 420, 32)
    $g.DrawString("⚡ [Auto Mua]: Đang mua Thỏi Vàng... (Đã mua: 4.000 / 10.000 cái)", $fontBold, [System.Drawing.Brushes]::Lime, 252, 82)

    # Standard Bottom Right Game Action Buttons (Capsule, Zone, Fusion...)
    $bx = 760; $by = 440
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 60, 65, 75))), $bx, $by, 45, 45)
    $g.DrawEllipse([System.Drawing.Pens]::Gold, $bx, $by, 45, 45)
    $g.DrawString("Khu", $fontSmallBold, [System.Drawing.Brushes]::White, $bx + 11, $by + 14)

    $bx -= 60
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 60, 65, 75))), $bx, $by, 45, 45)
    $g.DrawEllipse([System.Drawing.Pens]::Gold, $bx, $by, 45, 45)
    $g.DrawString("Cap", $fontSmallBold, [System.Drawing.Brushes]::White, $bx + 11, $by + 14)

    # NEW HUD BUTTON: [⚡ AMN] (Auto Mua Nhanh)
    $btnAmnX = $bx - 70; $btnAmnY = $by - 10
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 200, 90, 0))), $btnAmnX, $btnAmnY, 56, 56)
    $g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::Yellow, 3)), $btnAmnX, $btnAmnY, 56, 56)
    $g.DrawString("⚡AMN", $fontTitle, [System.Drawing.Brushes]::White, $btnAmnX + 5, $btnAmnY + 18)

    # LED Indicator (Green Active)
    $g.FillEllipse([System.Drawing.Brushes]::Lime, $btnAmnX + 40, $btnAmnY + 4, 12, 12)
    $g.DrawEllipse([System.Drawing.Pens]::White, $btnAmnX + 40, $btnAmnY + 4, 12, 12)

    # POPUP MENU (Bung lên khi bấm nút AMN hoặc phím tắt)
    $popX = $btnAmnX - 220; $popY = $btnAmnY - 240; $popW = 270; $popH = 225
    $popBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(240, 20, 22, 28))
    $g.FillRectangle($popBg, $popX, $popY, $popW, $popH)
    $popBg.Dispose()
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::Gold, 2)), $popX, $popY, $popW, $popH)

    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 45, 50, 60))), $popX + 2, $popY + 2, $popW - 4, 28)
    $g.DrawString("⚡ MENU AUTO MUA NHIỀU", $fontBold, [System.Drawing.Brushes]::Orange, $popX + 10, $popY + 7)

    # Menu Options
    $opts = @(
        @{Txt="▶ BẬT AUTO (Đang chọn: Thỏi Vàng)"; Clr=[System.Drawing.Color]::LimeGreen},
        @{Txt="🎯 Đổi vật phẩm mục tiêu từ Shop"; Clr=[System.Drawing.Color]::Yellow},
        @{Txt="⚙ Cài đặt: 1.000 / đợt (Delay 150ms)"; Clr=[System.Drawing.Color]::White},
        @{Txt="🔢 Đặt số đợt: 10 đợt (Tổng: 10k)"; Clr=[System.Drawing.Color]::LightSkyBlue},
        @{Txt="⏹ Dừng khẩn cấp (Phím Q)"; Clr=[System.Drawing.Color]::Crimson}
    )

    $oy = $popY + 36
    foreach ($o in $opts) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 35, 40, 48))), $popX + 8, $oy, $popW - 16, 32)
        $g.DrawRectangle([System.Drawing.Pens]::DimGray, $popX + 8, $oy, $popW - 16, 32)
        $g.DrawString($o.Txt, $fontSmallBold, (New-Object System.Drawing.SolidBrush $o.Clr), $popX + 15, $oy + 8)
        $oy += 36
    }

    # Watermark Mẫu 3
    $tagBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 20, 20, 20))
    $g.FillRectangle($tagBrush, 15, $h - 38, 440, 28)
    $g.DrawRectangle([System.Drawing.Pens]::Gold, 15, $h - 38, 440, 28)
    $g.DrawString("MẪU 3: NÚT NỔI TRỢ NĂNG HUD & POPUP MENU", $fontBold, [System.Drawing.Brushes]::Yellow, 25, $h - 32)
    $tagBrush.Dispose()

    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $g.Dispose()
}

$dir1 = "c:\Users\admin\Documents\Nro2\Kehoach"
$dir2 = "C:\Users\admin\.gemini\antigravity\brain\9357161a-5709-4dec-ba5c-14c09f3f8682"

if (-not (Test-Path $dir1)) { New-Item -ItemType Directory -Path $dir1 -Force | Out-Null }
if (-not (Test-Path $dir2)) { New-Item -ItemType Directory -Path $dir2 -Force | Out-Null }

Write-Host "Creating Mockup 1..."
Create-Mockup1 "$dir1\menu_mau_1.png"
Copy-Item "$dir1\menu_mau_1.png" "$dir2\menu_mau_1.png" -Force

Write-Host "Creating Mockup 2..."
Create-Mockup2 "$dir1\menu_mau_2.png"
Copy-Item "$dir1\menu_mau_2.png" "$dir2\menu_mau_2.png" -Force

Write-Host "Creating Mockup 3..."
Create-Mockup3 "$dir1\menu_mau_3.png"
Copy-Item "$dir1\menu_mau_3.png" "$dir2\menu_mau_3.png" -Force

Write-Host "Completed generating 3 mockups!"
Get-ChildItem $dir1 -Filter "menu_mau_*.png" | Select-Object Name, Length
