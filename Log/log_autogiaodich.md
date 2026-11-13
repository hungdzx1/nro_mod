# Nhật ký triển khai Auto Giao Dịch Nhanh (`AutoGiaoDich`)

## 1. Mục tiêu
Xây dựng chức năng **Auto Giao Dịch Nhanh** (`Z -> GD nhanh`, phím nóng dừng **`F6`**) trong file module riêng, hỗ trợ cả **Người GỬI (`Sender`)** và **Người NHẬN (`Receiver`)**, tự động mở/nhận giao dịch, bỏ vật phẩm theo số lượng cấu hình, khóa và xác nhận với delay riêng cho từng bước, ẩn Panel giao dịch (tùy chọn) và dừng an toàn khi số lượng vật phẩm trong rương nhỏ hơn số lượng chọn hoặc gặp điều kiện dừng.

## 2. Phạm vi file thay đổi (giữ nguyên toàn bộ logic gốc khác)
- **File mới tạo:**
  - `DecompiledSource/Game1.Mod/AutoGiaoDich.cs`
  - `DecompiledSource/Game2.Mod/AutoGiaoDich.cs`
  - `MyMod/AutoGiaoDich_Game1.cs`
  - `MyMod/AutoGiaoDich_Game2.cs`
- **Hook tối thiểu tại các file hệ thống (`Game1` & `Game2`):**
  - `ModFunc.cs`: `case 122:` (phím `Z`) dựng menu gồm `"Auto nhanh"`, `"Bật/Tắt auto nhanh"` (`AutoDungNhanh`) và `"GD nhanh"` (`AutoGiaoDich.gI()`, action `100`); gọi `AutoGiaoDich.gI().Update()` và `AutoGiaoDich.gI().Paint(g)`.
  - `Controller.cs`:
    - `case -86:` (`b13 == 0` gọi `OnReceiveInvite`, `b13 == 1` gọi `OnTradeOpened`, `b13 == 2` gọi `OnMyItemRemoved`, `b13 == 6` đọc đủ byte stream và bỏ qua cập nhật Panel nếu `ShouldSuppressPanelUi()` rồi gọi `OnFriendLocked`, `b13 == 7` gọi `OnTradeClosed` trước `panel.hide()`).
    - `case -36:`: gọi `OnBagUpdated()` sau khi cập nhật `arrItemBag`.
  - `InfoMe.cs`: gọi `OnServerInfo(s)`.
  - `ServerListScreen.cs`: gọi `OnDisconnectOrReset()`.
- **Backup script:** `MyMod/do_backup.ps1` thêm `Game1.Mod\AutoGiaoDich.cs` và `Game2.Mod\AutoGiaoDich.cs`.

## 3. Tính năng & máy trạng thái
- **Vai trò:**
  - `Sender`: Kiểm tra `GetBagItemCount(targetItemId) < qtyPerTrade` trước khi gửi mời và sau mỗi lượt hoàn tất; hỗ trợ gom/tách qua nhiều ô rương (`arrItemBag`); gửi `giaodich(0)` -> chờ mở (`-86, 1`) -> bỏ đồ (`giaodich(2)`) -> đợi `delayLockMs` -> khóa (`giaodich(5)`) -> chờ đối tác khóa (`-86, 6`) -> đợi `delayAcceptMs` -> xác nhận (`giaodich(7)`) -> chờ đóng (`-86, 7`) -> đợi `delayBetweenTradesMs` + đồng bộ `-36`.
  - `Receiver`: Lọc đúng `playerID == targetCharId` tại `b13 == 0`, tự đồng ý (`giaodich(1)`), đợi `delayLockMs` rồi khóa (`giaodich(5)`), đợi đối tác khóa -> đợi `delayAcceptMs` -> xác nhận (`giaodich(7)`), xong lượt tự quay lại `ReceiverWaitingInvite`.
- **Ẩn Panel khi GD (`hidePanelDuringTrade`, mặc định `true`):** Tùy chọn qua checkbox trên cửa sổ; ẩn cả `PopUpYesNo` mời GD và `GameCanvas.panel` giao dịch khi đang chạy auto với đúng đối tác.
- **Điều kiện dừng:** Phím **`F6`**, nút Dừng (gửi `giaodich(3)` nếu phiên đang mở dở), `GetBagItemCount(targetItemId) < qtyPerTrade`, đạt `targetLoops > 0`, `b13 == 2`, lỗi từ server (đầy hành trang / không thể GD / đạt giới hạn), đối tác rời map, hết số lần mời lại, hoặc ngắt kết nối.

## 4. Kiểm thử, Build & Backup
- **C# Test (`AutoGiaoDichTests`):** **13/13 PASS** cho cả `Game1.Mod.AutoGiaoDich` và `Game2.Mod.AutoGiaoDich`.
- **Kiểm tra đồng bộ `Game1` / `Game2` / `MyMod`:** PASS 100%.
- **Build (`MyMod/build_mod.ps1`):** `0 Error(s)` (0 cảnh báo trong `AutoGiaoDich.cs`), cập nhật `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1,810,432` bytes).
- **Backup (`MyMod/do_backup.ps1`):** `Backup/Backup_Snapshot_20261114_042611.zip` (`2,117,004` bytes).
