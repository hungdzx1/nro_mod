# Nhật ký phiên bản (`log_version.md`)

> Quy tắc: Chỉ ghi/cập nhật file này khi người dùng yêu cầu trực tiếp.

## [14/11/2026 04:26:40 (+07:00)] — Update giao dich nhan

- **Thông báo phiên bản:** **Update giao dich nhan** (`v3.1`)
- **Thời gian ghi log:** `14/11/2026 04:26:40 (+07:00)` (`2026-11-14`)
- **Git Commit dự kiến:** `"GD nhanh, Auto dùng nhanh, tối ưu auto gộp,... v3.1"` (nhánh `main` -> `origin/main`)
- **Backup Snapshot:** `Backup/Backup_Snapshot_20261114_042611.zip` (lúc `14/11/2026 04:26:31`, `2,117,004` bytes)
- **Build DLL:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (lúc `14/11/2026 04:10:46`, `1,810,432` bytes, build `0 Error(s)`, `0` cảnh báo trong `AutoGiaoDich.cs`)
- **Tóm tắt thay đổi & trạng thái:**
  1. **Auto Giao Dịch Nhanh (`AutoGiaoDich` — `Z -> GD nhanh`, dừng `F6`):**
     - Tạo module mới độc lập `DecompiledSource/Game1.Mod/AutoGiaoDich.cs`, `DecompiledSource/Game2.Mod/AutoGiaoDich.cs` cùng bản đồng bộ `MyMod/AutoGiaoDich_Game1.cs`, `MyMod/AutoGiaoDich_Game2.cs` (không chỉnh sửa logic gốc của `AutoDungNhanh.cs`, `AutoGop.cs`, `Panel.cs`, `GameScr.cs`, `Service.cs`).
     - Hỗ trợ 2 vai trò **Gửi (`Sender`)** và **Nhận (`Receiver`)** tự động hoàn toàn: bên Nhận lọc đúng `targetCharId` (`b13 == 0`), tự đồng ý mời (`giaodich(1)`), tự khóa (`giaodich(5)`), tự xác nhận (`giaodich(7)`) và lặp lại lượt kế tiếp.
     - Hỗ trợ chọn đối tác trong map / `[Target]`, chọn vật phẩm trong rương / nhập `ID`, đặt `SL/lượt`, `Số lượt`, 3 mức delay (`Delay GD`, `Delay khóa`, `Delay XN`), checkbox `Ẩn Panel` (mặc định bật), dừng khi số item trong rương `< SL chọn`, phím `F6`, lỗi server hoặc rời map.
     - Gắn hook tối thiểu tại `ModFunc.cs` (`case 122:` phím `Z`, `Update`, `Paint`), `Controller.cs` (`case -86:`, `case -36:`), `InfoMe.cs`, `ServerListScreen.cs` và cập nhật `MyMod/do_backup.ps1`.
  2. **Kiểm thử & đồng bộ (`Game1` & `Game2`):**
     - `AutoGiaoDichTests`: **13/13 PASS (0 FAILED)** bao phủ cả `Game1` và `Game2`.
     - Kiểm tra đồng bộ `Game1` / `Game2` / `MyMod` và hook: PASS 100%.
  3. **Chi tiết kỹ thuật:** xem `Log/log_autogiaodich.md`.

## [14/11/2026 03:09:40 (+07:00)] — Đã hoàn tất chức năng dùng nhanh + auto gộp

- **Thông báo phiên bản:** **Đã hoàn tất chức năng dùng nhanh + auto gộp**
- **Thời gian ghi log:** `14/11/2026 03:09:40 (+07:00)` (`2026-11-14`)
- **Git trạng thái:** Working tree hiện tại trên nền commit `c2087cc` (nhánh `main`, chưa commit/push phiên bản mới)
- **Backup Snapshot:** `Backup/Backup_Snapshot_20261114_030925.zip` (lúc `14/11/2026 03:09:40`, `2,067,364` bytes)
- **Build DLL:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (lúc `14/11/2026 03:02:56`, `1,775,616` bytes, build `0 Error(s)`, `0` cảnh báo trong `AutoDungNhanh.cs` và `AutoGop.cs`)
- **Tóm tắt thay đổi & trạng thái:**
  1. **Auto Dùng Nhanh (`AutoDungNhanh` riêng lẻ):**
     - Bổ sung checkbox `"Chạy liên tục"` (`continuousRun`, mặc định tắt): tự động bỏ tick `"Xóa khi hết"`, kích hoạt chạy ngay nếu bảng có `>= 1` vật phẩm đang bật, giữ trạng thái chờ dùng tiếp khi rương tạm hết đồ, ưu tiên dừng an toàn khi đầy hành trang (`stopOnBagFull`), dừng ngay bằng nút Dừng / `Z -> Tắt auto nhanh`, và tự tắt khi mất kết nối / về Login (`OnDisconnectOrReset`).
  2. **Auto Gộp (`AutoGop` — chỉ cập nhật giá trị mặc định + placeholder, giữ nguyên toàn bộ logic):**
     - Tab Bánh: `delayBuyBanhMs = 600` (giữ `delayUseBanhMs = 200`), `stopConditionBanh = 2`, `targetItemCountBanh = 2000000`.
     - Tab HN: `targetBatchesHn = 0` (`0 = vô hạn`), `delayBuyHnMs = 600`.
     - Tab Điểm: `pointIndex = 0` (`10M` / `10Tr`), `targetBatchesDiem = 1`.
  3. **Kiểm thử tổng hợp (`Game1` & `Game2`):**
     - `AutoDungNhanhTests`: `Game1` **9/9 PASS**, `Game2` **9/9 PASS**.
     - `AutoGop144724Tests`: `Game1` **19/19 PASSED (0 FAILED)**, `Game2` **19/19 PASSED (0 FAILED)**.
  4. **Chi tiết kỹ thuật từng hạng mục:** xem `Log/log_autodungnhanh.md` và `Log/log_autogop.md`.

## [14/11/2026 02:08:05 (+07:00)] — Phiên bản thêm Auto Dùng Nhanh (`AutoDungNhanh` riêng lẻ, phím `Z`)

- **Thời gian ghi log:** `14/11/2026 02:08:05 (+07:00)` (`2026-11-14`)
- **Git trạng thái:** Working tree chưa commit/push (trên nền commit `c2087cc`, nhánh `main`)
- **Backup Snapshot mới nhất:** `Backup/Backup_Snapshot_20261114_020745.zip` (lúc `14/11/2026 02:07:58`, `2,065,480` bytes) — bản cùng cấu hình trước đó: `Backup/Backup_Snapshot_20261113_205250.zip` (`13/11/2026 20:53:09`, `2,065,480` bytes)
- **Build DLL:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (lúc `13/11/2026 20:52:36`, `1,774,592` bytes, build `0 Error(s)`, `0` cảnh báo trong `AutoDungNhanh`)
- **Tóm tắt thay đổi & trạng thái:**
  1. Tạo module mới hoàn toàn độc lập `AutoDungNhanh` (`DecompiledSource/Game1.Mod/AutoDungNhanh.cs`, `DecompiledSource/Game2.Mod/AutoDungNhanh.cs` và bản đồng bộ trong `MyMod/`), giữ nguyên không đụng vào `AutoItem.cs`, `ModFunc.UseItemAuto()`, `ModFunc.OpenMenu()` (phím `X`), phím `Q`, nút `"QS"`, và `AutoGop.cs`.
  2. Tích hợp nút `"Auto\ndùng nhanh"` / `"Tắt auto\ndùng nhanh"` trong Hành trang (`Game1/Panel.cs` & `Game2/Panel.cs`), thêm/bật ngay theo `delayMs` hiện tại không hiện hộp hỏi delay.
  3. Gắn phím `Z` (`case 122:` trong `Game1/ModFunc.cs` & `Game2/ModFunc.cs`) mở Quick Menu (`GameCanvas.menu.startAt(myVector, 4)`) có `"Auto nhanh"` và `"Bật/Tắt auto nhanh"`.
  4. Bảng `AutoDungNhanh` hỗ trợ chỉnh trực tiếp `Delay (ms)` (`50..60000 ms`, mặc định `200 ms`), xoay vòng nhiều item (`indexUI`), phân trang 4 dòng/trang, checkbox bật/tắt từng item, `Dừng đầy rương` (`InfoMe.cs`), `Xóa khi hết`, và tự dừng/đóng bảng khi ngắt kết nối (`ServerListScreen.cs`).
  5. Cập nhật `MyMod/do_backup.ps1` và ghi chi tiết triển khai tại `Log/log_autodungnhanh.md`.
  6. Kiểm thử lại `AutoDungNhanhTests`: `Game1` `8/8 PASS`, `Game2` `8/8 PASS`.

## [13/11/2026 19:18:00 (+07:00)] — Phiên bản Auto quy trình thành công (chưa có dừng nhặt / dừng nhân)

- **Thời gian ghi log:** `13/11/2026 19:18:00 (+07:00)` (`2026-11-13`)
- **Git Commit:** `c2087cc` (nhánh `main` / `origin/main`, lúc `2026-11-13 18:32:01 +07:00`) — `"auto qua trinh thanh cong, chua co dung nhan"`
- **Backup Snapshot:** `Backup/Backup_Snapshot_20261113_182907.zip` (lúc `13/11/2026 18:29:28`, `2,022,293` bytes)
- **Phiên bản nền khôi phục:** `Backup/Backup_Snapshot_20261112_144724` (lúc `12/11/2026 14:47:29`, `1,196,267` bytes)
- **Build DLL:** `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (lúc `13/11/2026 17:34:53`, `1,758,208` bytes, build `0 Error(s)`)
- **Tóm tắt thay đổi & trạng thái:**
  1. Khôi phục toàn bộ mã nguồn và dữ liệu về `Backup_Snapshot_20261112_144724`.
  2. Thêm mục chọn **số vòng lặp quy trình** tại Tab `Chung` (`0 = vô hạn`), cập nhật UI hiển thị số vòng, logic rẽ nhánh quay về tab đầu và bảo vệ dừng an toàn khi vòng không phát sinh giao dịch.
  3. Sửa lỗi **nháy điểm `10M` (`10.000.000`) ↔ điểm hiện có** tại Tab `Điểm` (truyền `npcId` và `menu` từ `Controller.cs`, lọc đúng `npcId == 66` Menu cấp 1 `"đổi điểm"`, loại bỏ Menu cấp 2, bóc tag màu `|...|`, dùng `long`), sửa UTF-8 mojibake cho `Game2` và đồng bộ `Game1` / `Game2` / `MyMod`.
  4. Cập nhật `MyMod/do_backup.ps1` sao lưu nội bộ trong thư mục dự án và ghi chi tiết vào `Log/log_autogop.md`.
  5. Kiểm thử mô phỏng C#: `18/18 PASS` cho `Game1`, `18/18 PASS` cho `Game2`; `5/5` kiểm tra tĩnh PASS.
  6. **Ghi chú:** Auto quy trình hoạt động thành công; chưa có tính năng dừng nhặt / dừng nhân (`chua co dung nhan`).

