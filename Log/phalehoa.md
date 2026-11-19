# Nhật ký triển khai Auto Pha Lê Hóa Mới (`AutoPhaLe`)

## 1. Mục tiêu
Nâng cấp chức năng **Tự Động Pha Lê Hóa** bằng module độc lập `AutoPhaLe` (đồng bộ giữa `Game1` và `Game2`), mở bảng cấu hình bằng phím **`I`** hoặc **`Z -> Pha lê nhanh`**, dừng nhanh bằng **`F6`**, thay cơ chế `Thread.Sleep` cũ bằng **State Machine đồng bộ gói tin (Event-Driven)**, hỗ trợ chọn mức đập **`100 lần` / `10 lần` / `1 lần`** (mặc định `100 lần`), tự bán thỏi vàng có chống kẹt trần vàng server, đập theo danh sách nhiều món (Queue), ẩn hiệu ứng/menu trung gian và cho phép bật/tắt HUD cũ.

---

## 2. Phạm vi tệp đã tạo mới & chỉnh sửa

### Tệp tạo mới
- `DecompiledSource/Game1.Mod/AutoPhaLe.cs`
- `DecompiledSource/Game2.Mod/AutoPhaLe.cs`
- `MyMod/AutoPhaLe_Game1.cs`
- `MyMod/AutoPhaLe_Game2.cs`
- `MyMod/AutoPhaLeTests.ps1`

### Điểm gắn Hook (`Game1` & `Game2`)
- `ModFunc.cs`:
  - `UpdateKey(int key)`: thêm `case 105:` (phím `I`) gọi `AutoPhaLe.gI().ToggleWindow()`; thêm mục `"Pha lê nhanh"` trong `case 122:` (menu phím `Z`).
  - `Update()`: gọi `AutoPhaLe.gI().Update()`.
  - `Paint(mGraphics g)`: điều kiện vẽ HUD chữ đỏ cũ dùng `AutoPhaLe.gI().ShouldShowOldHud(isAutoPhaLe)` (bảo toàn nhánh `isAutoVQMM`); gọi `AutoPhaLe.gI().Paint(g)`.
- `Panel.cs`:
  - `doFireCombine()` & `doFireInventory()`: hiển thị nút `"Thêm vào\nDS Pha Lê"` / `"Xóa khỏi\nDS Pha Lê"` (action `1` cho Tab đang chọn) và `"Đặt làm\nNL đi kèm"` / `"Bỏ chọn\nNL đi kèm"` (action `3` cho Tab 2) khi `AutoPhaLe.gI().allowAddFromBag == true`, hỗ trợ cả `panel` (`position == 0`) lẫn `panel2` (`position == 1`) và dùng `AutoPhaLe.CanAddItem(currItem)`.
  - `perform` (`idAction == 6000`): gọi `AutoPhaLe.gI().OnCombineSlotItemAdded(item2, GameCanvas.panel.vItemCombine.size())` để tự nạp món chính (và gán nguyên liệu kèm nếu bỏ món thứ 2 ở Tab 2).
  - `DoFireModFunc()`: khi bật Auto Pha Lê cũ (`ModFunc.GI().isAutoPhaLe = true`), gọi `AutoPhaLe.gI().OnOldAutoToggledOn()` để tắt bản mới.
- `Controller.cs`:
  - `case -81:` (`b30 == 0..8`): đọc đủ byte từ `msg.reader()`; nếu `AutoPhaLe.gI().ShouldSkipCombineEffect()` là `true` thì **chặn mở lại bảng Combine** (`setTypeCombine()`, `show()`, `setTabCombine()` ở `b30 == 0, 1`) và **bỏ qua mọi hiệu ứng** `setCombineEff(...)` (`b30 == 2..8`); gọi `AutoPhaLe.gI().OnCombinePacket(b30)` cho `b30 == 2..8`.
  - `case -36:`: gọi `AutoPhaLe.gI().OnBagUpdated()` sau khi cập nhật hành trang.
  - `case 32:` (cả 2 nhánh `npc4` và `npc5`): gọi `AutoPhaLe.gI().OnNpcMenu(...)` để chọn option xác nhận ngay khi nhận menu từ NPC `21` (Bà Hạt Mít) và ẩn `createMenu` / `ChatPopup` nếu `hideUi == true`.
  - `case 38:` (`OPEN_UI_SAY`, cả 2 nhánh `npc2` và `npc3`): gọi `AutoPhaLe.gI().OnNpcSay(num159, str6)` để xử lý thông báo/lỗi từ Bà Hạt Mít và bỏ qua `ChatPopup` + `panel.hideNow()` khi `hideUi == true`.
- `InfoMe.cs`: gọi `AutoPhaLe.gI().OnServerInfo(s)` để xử lý thông báo thiếu vàng, hết ngọc, đầy hành trang, tối đa sao.
- `ServerListScreen.cs`: gọi `AutoPhaLe.gI().OnDisconnectOrReset()` khi ngắt kết nối hoặc về màn hình chọn máy chủ.
- Đóng chéo cửa sổ (`isShowWindow = false`): cập nhật `AutoDungNhanh.cs`, `AutoGiaoDich.cs`, `AutoGop.cs` (`Game1.Mod` & `Game2.Mod`) và `AutoDoanhTrai.cs` (`Game1.Mod`).
- `MyMod/do_backup.ps1`: bổ sung `'Game1.Mod\AutoPhaLe.cs'` và `'Game2.Mod\AutoPhaLe.cs'`.

---

## 3. Tính năng & Luồng hoạt động chi tiết

### A. State Machine đồng bộ gói tin (`State`)
Luồng chạy trên main thread qua `Update()` gồm 6 trạng thái:
`Idle` -> `CheckAndPrepare` -> (`SellGold`) -> `SendCombine` -> `WaitMenu` -> `WaitResult` -> `CheckAndPrepare`.

1. **`CheckAndPrepare`:**
   - Kiểm tra map Đảo Kame (`TileMap.mapID == 5`); không kiểm tra dừng theo tổng `luong + luongKhoa` (tránh tràn số 32-bit `int` khi ngọc lớn hơn 2,14 tỷ, thiếu ngọc được xử lý qua thông báo Server trong `OnServerInfo` giống luồng Auto Pha Lê cũ).
   - Đồng bộ số sao hiện có (`optionTemplate.id == 107`) từ `arrItemBag`; món đạt `currStar >= targetStar` hoặc `maxedByServer` được giữ lại trong danh sách với trạng thái `[Đủ sao]`.
   - Kiểm tra `xu < minGoldBillion * 1 tỷ` (hoặc server báo thiếu vàng) để chuyển sang `SellGold`.
2. **`SellGold` (Chống mất thỏi vàng khi chạm trần server):**
   - Dùng thỏi vàng (`id == 457`) theo `sellGoldDelayMs` cho đến khi đạt `maxGoldBillion * 1 tỷ`.
   - Lưu `lastGoldBeforeUse` và `lastGoldBarCount` trước mỗi lần bán; nếu sau khi phản hồi mà `xu` không tăng `2` lần liên tiếp (`goldStallCount >= 2`), tự động dừng bán và quay lại tiếp tục đập.
3. **`SendCombine` & `WaitMenu`:**
   - Chờ đủ `upgradeDelayMs` kể từ kết quả trước, tự di chuyển lại gần NPC Bà Hạt Mít (`npcTemplateId == 21`) nếu khoảng cách `>= 50`, đồng bộ `vItemCombine` và gửi `Service.gI().combine(1, combineVec)`.
   - Chờ gói `cmd 32`; nếu quá `menuRetryDelayMs` tự gửi lại (tối đa `15` lần).
4. **Xác nhận tức thì, Chọn nút đầu ở Tab 2 & Chọn mức đập ở Tab 1:**
   - Phát hiện `IsMainNpcMenu(menu)` (khi người chơi chưa chọn sẵn tính năng ở Bà Hạt Mít mà mở nhầm menu danh sách chức năng chính) để tự dừng an toàn kèm hướng dẫn.
   - **Tab 1 (`runningTab == 0`):** Tìm nút nâng cấp theo chế độ chọn (`100 lần` -> `10 lần` -> `1 lần`, mặc định `100 lần`) qua `SelectUpgradeMenuIndex`, bỏ qua các nút hủy (`"từ chối"`, `"đóng"`, `"hủy"`, `"quay lại"`, `"thoát"`).
   - **Tab 2 (`runningTab == 1`):** Luôn chọn **nút đầu tiên hợp lệ (`index = 0`, VD: `"Làm phép"`, `"Nâng cấp"`)** qua `SelectFirstConfirmMenuIndex(menu)` bất kể menu có 1 hay nhiều nút.
   - **Chỉ kiểm tra kịch cấp khi không có nút nâng cấp (`selectedIndex < 0`) hoặc qua `OnServerInfo` / `OnNpcSay`**, và siết chặt `IsMaxLevelReachedText` (`"đạt cấp tối đa"`, `"đã đạt tối đa"`, `"cấp tối đa"`, `"sao tối đa"`, `"pha lê tối đa"`, `"hóa tối đa"`, `"hoá tối đa"`, `"đã tối đa"`, `"tối đa rồi"`, `"đã đạt cấp"`, `"không thể nâng cấp"`, `"đạt giới hạn"`, `"cấp cao nhất"`) để không bao giờ nhận nhầm dòng chỉ số vật phẩm (VD: `"Linh Khí Tối Đa +3300"`, `"HP tối đa"`, `"KI tối đa"`).
   - Loại bỏ toàn bộ `Sound.start(...)` theo yêu cầu tắt âm thanh; bọc mọi thông báo nội bộ qua `NotifyInfo` với cờ `isAddingInternalInfo` để chặn đệ quy ngược từ `InfoMe.addInfo` -> `OnServerInfo`.
   - `HideCombinePanelsIfNeeded()` và nhánh `type == 12` của `CloseOtherPanels()` đặt trực tiếp `panel.isShow = false`, `GameScr.isPaint = true` thay vì gọi `hideNow()`, tránh chạy `mSystem.gcc()` (`Resources.UnloadUnusedAssets()`) và tránh xóa `vItemCombine`.
5. **`WaitResult`:**
   - Chờ gói kết quả (`cmd -81`, `b30 == 2/3`) và cập nhật túi đồ (`cmd -36`), hoặc tự fallback sau timeout `3000ms` để tránh kẹt trạng thái.

### B. Panel 2 Tab (`Phím I` / `Z -> Pha lê nhanh`) & Lưu cài đặt (`AutoSettingsStore`)
Cấu hình được lưu tự động theo cửa sổ (`"Game1.AutoPhaLe"` / `"Game2.AutoPhaLe"`):
- **Tab 1 (`Pha Lê Hóa` — `selectedTab == 0`):**
  - `defaultTargetStar` (`7`), `upgradeBatchMode` (`100` / `10` / `1`, mặc định `100`), `upgradeDelayMs` (`150` ms), `menuRetryDelayMs` (`200` ms).
  - Hiển thị chỉ số ngọc riêng biệt (`"NX:" + NinjaUtil.getMoneys(luong) + " NH:" + NinjaUtil.getMoneys(luongKhoa)`), hỗ trợ chỉnh sao riêng từng món (`*`), `showOldHud` (mặc định `false`).
- **Tab 2 (`Nâng Cấp Chung` — `selectedTab == 1`):**
  - Dùng chung cho các chức năng khác của Bà Hạt Mít (*Tinh ấn*, *Pháp sư hoá*, *Bóng tối hoá*, *Ánh sáng hoá*, *Ép sao*, *Nâng cấp Lệnh bài...*).
  - Menu xác nhận ở Tab 2 cố định là **Nút đầu (Làm phép)** (`SelectFirstConfirmMenuIndex` chọn `index 0`), chỉnh riêng được `generalUpgradeDelayMs` (`150` ms) và `generalMenuDelayMs` (`200` ms).
  - Hàng đợi trang bị riêng `generalQueue` + tùy chọn **Nguyên liệu đi kèm** (`companionMaterialId`, `companionMaterialName`): chọn qua nút `Chọn NL` ngay trên Panel hoặc `"Đặt làm\nNL đi kèm"` từ Hành trang; tự động gắn kèm mỗi lượt combine, nếu không chọn (`-1`) thì chỉ bỏ 1 mình Trang bị chính. Đã chặn tuyệt đối vật phẩm trùng `companionMaterialId` và ô thứ 2 trở đi trên bàn Combine lọt vào `generalQueue` (kể cả khi `companionMaterialId` đã được chọn trước), đồng thời `SetCompanionMaterial` tự động dọn các mục trùng khỏi `generalQueue`.
  - Tự động chuyển sang món tiếp theo khi gặp thông báo kịch cấp thực sự (`IsMaxLevelReachedText` qua `OnServerInfo`, `OnNpcSay`, hoặc menu chỉ còn nút hủy), và tự dừng khi hết nguyên liệu kèm / thiếu ngọc / thiếu đá / hết vàng / từ chối từ Bà Hạt Mít (`case 38`).
- **Thông số dùng chung & Tiện ích:**
  - `sellGoldDelayMs` (`120` ms), `minGoldBillion` (`10` Tỷ), `maxGoldBillion` (`60` Tỷ), `allowAddFromBag` (`true`), `hideUi` (`true` — đóng `GameCanvas.panel` & `GameCanvas.panel2` + chặn mở lại bảng Combine & hiệu ứng khi đang chạy).
  - Chọn đồ & nguyên liệu trực tiếp trên Panel (`pickerMode == 1 / 2`) qua nút **`+ Chọn đồ`** / **`+ Chọn TB`** / **`Chọn NL`**.
  - Loại trừ lẫn nhau với bản cũ (`ModFunc.GI().isAutoPhaLe`).

---

## 4. Kết quả Kiểm thử, Build & Backup
- **Kiểm thử (`MyMod/AutoPhaLeTests.ps1`):** **147/147 PASS (0 FAILED)** — kiểm tra đồng bộ `Game1`/`Game2`/`MyMod`, kiểm tra tắt `Sound.start`, chống đệ quy `NotifyInfo`/`isAddingInternalInfo`, `HideCombinePanelsIfNeeded` không gọi `hideNow()`, hook `Controller.cs` cho `case -81` (`b30 == 0..8`) & `case 38` (`OnNpcSay`), chặn vật phẩm nguyên liệu đi kèm lọt vào `generalQueue`, và chạy C# simulation cho `SelectUpgradeMenuIndex`, `SelectFirstConfirmMenuIndex` (Tab 2 luôn chọn nút đầu `0`), `IsMainNpcMenu`, `IsMaxLevelReachedText` (không bắt nhầm `"Linh Khí Tối Đa +3300"`, `"HP tối đa"`, `"KI tối đa"`), chống đệ quy `NotifyInfo` -> `OnServerInfo`, và quy trình Tab 2 với `Nguyên Anh` + `Ngôi sao Ánh Sáng`. (Chưa kiểm thử trực tiếp trong client game đang chạy với server).
- **Biên dịch (`MyMod/build_mod.ps1`):** `0 Error(s)`, `246 Warning(s)` sẵn có của dự án (`0` cảnh báo mới trong `AutoPhaLe.cs` / `Controller.cs`).
  - DLL đầu ra: `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1.909.248` byte, SHA-256 `2A40D503FFF218C373C6A4552516534CD907AFB88E70AE33133C716FE58A02DD`).
- **Sao lưu (`MyMod/do_backup.ps1`):** `Backup/Backup_Snapshot_20261118_150229.zip` (`3.156.344` byte).
