# Nhật ký Sao lưu (`Log/backup.md`)

## [18/11/2026 15:02:47 (+07:00)] — Backup Snapshot: `Backup_Snapshot_20261118_150229.zip`

- **File sao lưu:** `Backup/Backup_Snapshot_20261118_150229.zip` (`3.156.344` byte).
- **DLL được đóng gói:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1.909.248` byte, SHA-256 `2A40D503FFF218C373C6A4552516534CD907AFB88E70AE33133C716FE58A02DD`).
- **Build & Kiểm thử trước sao lưu:**
  - `MyMod/AutoPhaLeTests.ps1`: **147/147 PASS (0 FAILED)** (kiểm tra đồng bộ `Game1`/`Game2`/`MyMod`, kiểm tra tắt `Sound.start`, chống đệ quy `NotifyInfo`/`isAddingInternalInfo`, không gọi `hideNow()` trong `HideCombinePanelsIfNeeded()`, hook `case 38` `OnNpcSay`, chặn nguyên liệu kèm lọt vào `generalQueue`, C# simulation hồi quy cho `Nguyên Anh` / `"Linh Khí Tối Đa"` và chọn nút đầu Tab 2). Chưa kiểm thử trực tiếp trong client game đang chạy với server.
  - `MyMod/build_mod.ps1`: **`0 Error(s)`**, `246 Warning(s)` sẵn có (`0` cảnh báo mới).

### Những cập nhật mới có trong bản Backup này (so với `Backup_Snapshot_20261118_140419.zip`)
1. **Khắc phục lỗi treo game (`Not Responding`) và nhận nhầm `"Linh Khí Tối Đa"`:**
   - Siết chặt `IsMaxLevelReachedText` (bỏ kiểm tra `"tối đa"` đơn lẻ, chỉ khớp các cụm thông báo đạt cấp tối đa thực thụ như `"đạt cấp tối đa"`, `"sao tối đa"`, `"hóa tối đa"`, `"hoá tối đa"`, `"đã tối đa"`, `"không thể nâng cấp"`, `"đạt giới hạn"`), tránh nhận nhầm dòng chỉ số trang bị trên `Nguyên Anh` (`"Linh Khí Tối Đa +3300"`, `"HP tối đa"`, `"KI tối đa"`).
   - Bổ sung cơ chế `NotifyInfo` + cờ chống đệ quy `isAddingInternalInfo` ngăn vòng lặp vô tận `MarkActiveEntryMaxed` <-> `InfoMe.addInfo` <-> `OnServerInfo`.
   - Đổi `HideCombinePanelsIfNeeded()` (và nhánh `type == 12` của `CloseOtherPanels()`) sang ẩn nhẹ `panel.isShow = false` + `GameScr.isPaint = true` thay cho `hideNow()` (tránh gọi `mSystem.gcc()` / `Resources.UnloadUnusedAssets()` gây khựng và tránh xóa `vItemCombine`).
2. **Sửa lỗi báo hết nguyên liệu đi kèm ảo (`"Đã hết nguyên liệu đi kèm: Ngôi sao Ánh Sáng"`):**
   - Chặn tuyệt đối vật phẩm trùng `companionMaterialId` và ô thứ 2 trở đi trên bàn Combine lọt vào `generalQueue` trong `ImportCombineSnapshotToTab`, `TryImportFromCombineIfEmpty`, `OnCombineSlotItemAdded`, và `ToggleItemInTab` (bất kể đã chọn `companionMaterialId` từ trước hay chưa).
   - `SetCompanionMaterial` tự động xóa mọi mục trùng ID khỏi `generalQueue`; `FindBagItemById` bỏ điều kiện `quantity > 0` (đồng nhất với `FindBagItemIndexUI`) và có nhánh dự phòng khi trùng slot với `quantity > 1`.
3. **Sửa chọn nút đầu cho Tab 2 & Tắt toàn bộ âm thanh:**
   - Tab 2 (`Nâng Cấp Chung`) luôn chọn nút đầu tiên hợp lệ (`index = 0`, VD: `"Làm phép"`) qua `SelectFirstConfirmMenuIndex(menu)`, cập nhật giao diện dòng 1 hiển thị `"Nút đầu (Làm phép)"` và chỉ kiểm tra kịch cấp ở `OnNpcMenu` khi không có nút nâng cấp hợp lệ (`selectedIndex < 0`), đồng thời phát hiện & dừng an toàn nếu gặp menu chính nhiều chức năng của Bà Hạt Mít (`IsMainNpcMenu`).
   - Bổ sung `OnNpcSay(int npcId, string sayText)` hook vào `case 38` (`OPEN_UI_SAY`) ở cả `Game1/Controller.cs` và `Game2/Controller.cs`.
   - Xóa toàn bộ lời gọi `Sound.start(1f, Sound.l1)` trong `AutoPhaLe`.

---

## [18/11/2026 14:04:44 (+07:00)] — Backup Snapshot: `Backup_Snapshot_20261118_140419.zip`

- **File sao lưu:** `Backup/Backup_Snapshot_20261118_140419.zip` (`3.149.833` byte).
- **DLL được đóng gói:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1.906.176` byte, SHA-256 `A1181D788439D646767D2438474E605AA3EC53F02F29F574CEC964EE593B9F7B`).
- **Build & Kiểm thử trước sao lưu:**
  - `MyMod/AutoPhaLeTests.ps1`: **115/115 PASS (0 FAILED)** (kiểm tra đồng bộ `Game1`/`Game2`/`MyMod`, kiểm tra hook `Controller`/`Panel`, C# simulation Tab 1 & Tab 2). Chưa kiểm thử trực tiếp trong client game đang chạy với server.
  - `MyMod/build_mod.ps1`: **`0 Error(s)`**, `246 Warning(s)` sẵn có (`0` cảnh báo trong `AutoPhaLe.cs`).

### Những cập nhật mới có trong bản Backup này (so với `Backup_Snapshot_20261118_134522.zip`)
1. **Thêm Tab 2 (`Nâng Cấp Chung`) trên Panel `AutoPhaLe` (Phím `I` / `Z -> Pha lê nhanh`):**
   - Chia Panel `I` thành 2 Tab: **Tab 1 (`Pha Lê Hóa`)** và **Tab 2 (`Nâng Cấp Chung`)** dùng chung cho mọi chức năng bỏ đồ + xác nhận của Bà Hạt Mít (*Tinh ấn*, *Pháp sư hoá*, *Bóng tối hoá*, *Ánh sáng hoá*, *Ép sao*, *Nâng cấp Lệnh bài / Trang bị*...).
   - Cho phép chỉnh riêng `generalBatchMode` (`100 / 10 / 1 lần`), `generalUpgradeDelayMs`, `generalMenuDelayMs`, dùng chung cấu hình tự bán Thỏi Vàng, lưu tự động qua `AutoSettingsStore`.
   - Hỗ trợ hàng đợi nhiều **Trang bị chính** (`generalQueue`) + tùy chọn **Nguyên liệu đi kèm** (`companionMaterialId` / `companionMaterialName`) chọn ngay trên Panel (`+ Chọn TB`, `Chọn NL`, `Xóa`) hoặc từ Hành trang (`"Đặt làm\nNL đi kèm"`). Nếu không chọn nguyên liệu kèm thì chỉ bỏ 1 mình Trang bị chính; nếu có chọn thì tự tìm theo ID đính kèm vào ô Combine mỗi lượt và dừng ngay khi hết nguyên liệu.
   - Tự nhận diện thông báo kịch cấp (`IsMaxLevelReachedText`: `"tối đa"`, `"đã đạt cấp"`, `"không thể nâng cấp thêm"`, `"đạt giới hạn"`, `"cấp cao nhất"`) để tự chuyển sang món tiếp theo trong Queue; tự nhận diện menu 1 nút xác nhận (`index 0`) hoặc nhiều nút (`100 / 10 / 1 lần`).
2. **Ẩn hoàn toàn Panel Nâng cấp chống lag (cho cả Tab 1 & Tab 2):**
   - Khi `isRunning && hideUi`, tự động đóng cả `GameCanvas.panel` (trái) và `GameCanvas.panel2` (phải) qua `HideCombinePanelsIfNeeded()`.
   - Trong `Controller.cs` (`case -81`): vẫn đọc đủ byte stream nhưng chặn không gọi `setTypeCombine()`, `show()`, `setTabCombine()` ở `b30 == 0, 1` và bỏ qua mọi hiệu ứng `setCombineEff(...)` ở `b30 == 2..8`, đồng thời báo `OnCombinePacket(b30)` cho mọi mã kết quả `2..8`.

---

## [18/11/2026 13:45:42 (+07:00)] — Backup Snapshot: `Backup_Snapshot_20261118_134522.zip`

- **File sao lưu:** `Backup/Backup_Snapshot_20261118_134522.zip` (`3.134.150` byte).
- **DLL được đóng gói:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1.892.352` byte, SHA-256 `6C6D71042945B0A77CDAB6D0CB3367B5CA93727AC243AC9209831E4DE910237E`).
- **Build & Kiểm thử trước sao lưu:**
  - `MyMod/AutoPhaLeTests.ps1`: **95/95 PASS (0 FAILED)** (kiểm tra đồng bộ `Game1`/`Game2`/`MyMod`, kiểm tra hook, C# simulation). Chưa kiểm thử trực tiếp trong client game đang chạy với server.
  - `MyMod/build_mod.ps1`: **`0 Error(s)`**, `246 Warning(s)` sẵn có (`0` cảnh báo trong `AutoPhaLe.cs`).

---

### Những cập nhật mới có trong bản Backup này (so với snapshot `Backup_Snapshot_20261118_112632.zip`)

1. **Module mới: Tự Động Pha Lê Hóa Mới (`AutoPhaLe`) — đồng bộ `Game1` & `Game2`**
   - Tạo mới `DecompiledSource/Game1.Mod/AutoPhaLe.cs`, `DecompiledSource/Game2.Mod/AutoPhaLe.cs`, `MyMod/AutoPhaLe_Game1.cs`, `MyMod/AutoPhaLe_Game2.cs` và bộ test `MyMod/AutoPhaLeTests.ps1`.
   - Mở Panel mới bằng phím **`I`** (`case 105` trong `ModFunc.cs`) hoặc menu phím **`Z -> Pha lê nhanh`** (`case 122`); dừng nhanh bằng phím **`F6`**.
   - Chạy bằng **State Machine đồng bộ gói tin** (`CheckAndPrepare` -> `SellGold` -> `SendCombine` -> `WaitMenu` -> `WaitResult`), loại bỏ `Thread.Sleep` ở luồng phụ.
   - Cho phép tùy chỉnh trên Panel và tự lưu theo cửa sổ qua `AutoSettingsStore`:
     - Sao mục tiêu mặc định (`defaultTargetStar = 7`), chế độ đập **`100 lần` / `10 lần` / `1 lần`** (mặc định `100 lần`).
     - Các mức delay riêng: `Delay đập` (`150 ms`), `Delay menu` (`200 ms`), `Delay bán TV` (`120 ms`).
     - Ngưỡng vàng tính theo Tỷ (`Vàng < 10T -> bán đến 60T`) kèm cơ chế phát hiện chạm trần vàng server (`goldStallCount >= 2`) để tránh mất sạch thỏi vàng.
   - Tự động nhận diện menu Bà Hạt Mít (`npcId == 21`) ưu tiên đúng mức đập đang chọn (`100 lần` -> `10 lần` -> `1 lần`), tránh nút hủy (`Từ chối`, `Đóng`, `Hủy`...), tự dừng món khi gặp thông báo `"tối đa"`.
   - Tùy chọn ẩn hiệu ứng `setCombineEff`, `ChatPopup` & menu xác nhận (`hideUi = true`), và tùy chọn bật/tắt HUD chữ đỏ cũ (`showOldHud = false`, bảo toàn `isAutoVQMM`).
   - Loại trừ lẫn nhau với Auto Pha Lê cũ: bật bản mới tự tắt `ModFunc.GI().isAutoPhaLe`, bật bản cũ trong cài đặt tự gọi `OnOldAutoToggledOn()` để dừng bản mới; giữ nguyên code/logic bản cũ trong `ModFunc.AutoPhaLe()`.

2. **Bổ sung & Sửa lỗi chọn đồ đưa vào danh sách (Queue nhiều món)**
   - Hỗ trợ hàng đợi nhiều món (3 dòng/trang), chỉnh sao mục tiêu từng món (`*`), giữ lại món hoàn thành với trạng thái `[Đủ sao]`, xóa từng món / xóa đã xong / xóa tất cả.
   - Bỏ giới hạn `isTypeBody()` bằng hàm `CanAddItem(Item)` (nhận cả trang bị lẫn vật phẩm đặc thù như *Lệnh Bài Tạp Dịch*, loại trừ thỏi vàng `457`), đặt mặc định `allowAddFromBag = true`.
   - Tích hợp thêm/bỏ đồ từ cả `panel` (`position == 0`) lẫn `panel2` (`position == 1` khi mở bảng Combine trên PC), tự thêm món vào danh sách khi bấm `"Chọn dùng"` (`idAction == 6000`) và tự nạp món đang nằm sẵn trong ô Combine khi mở Panel `I`.
   - Thêm nút **`+ Chọn đồ`** (`isPickingFromBag`) cho phép xem và bấm chọn vật phẩm từ hành trang trực tiếp ngay trên Panel `I`.

3. **Sửa lỗi báo "Đã hết ngọc" khi số ngọc lớn (`> 2,14 tỷ`)**
   - Loại bỏ kiểm tra dừng gây tràn số 32-bit `int` (`Char.myCharz().luong + Char.myCharz().luongKhoa <= 0`) trong `AutoPhaLe.Update()`, chuyển sang xử lý hết ngọc theo thông báo từ Server qua `OnServerInfo(string s)`.
   - Sửa hiển thị ngọc trên Panel (Row 5) lấy riêng từng chỉ số (`"NX:" + NinjaUtil.getMoneys(Char.myCharz().luong) + " NH:" + NinjaUtil.getMoneys(Char.myCharz().luongKhoa)`) giống bản Auto Pha Lê ban đầu.

4. **Các điểm tích hợp hệ thống đi kèm (`Game1` & `Game2`)**
   - Gắn hook tại `ModFunc.cs`, `Panel.cs`, `Controller.cs`, `InfoMe.cs`, `ServerListScreen.cs`.
   - Cập nhật đóng chéo cửa sổ panel (`isShowWindow = false`) trong `AutoDungNhanh.cs`, `AutoGiaoDich.cs`, `AutoGop.cs` (`Game1.Mod` & `Game2.Mod`) và `AutoDoanhTrai.cs` (`Game1.Mod`).
   - Cập nhật `MyMod/do_backup.ps1` bao gồm `'Game1.Mod\AutoPhaLe.cs'` và `'Game2.Mod\AutoPhaLe.cs'`.
