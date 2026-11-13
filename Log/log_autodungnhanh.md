# Log Cập Nhật Auto Dùng Nhanh (`AutoDungNhanh`)

- **Thời gian:** 13/11/2026 20:53 (+07)
- **Trạng thái:** Hoàn tất triển khai độc lập, kiểm thử PASS 100%, biên dịch `Assembly-CSharp.dll` và sao lưu snapshot thành công.

## 1. Kiến trúc riêng lẻ (Không đụng code / logic cũ)
- Viết mới hoàn toàn lớp `AutoDungNhanh : IChatable, IActionListener` trong file riêng:
  - `DecompiledSource/Game1.Mod/AutoDungNhanh.cs`
  - `DecompiledSource/Game2.Mod/AutoDungNhanh.cs` (đồng bộ 100% logic với `Game1.Mod`)
  - Bản sao đồng bộ trong `MyMod/`: `MyMod/AutoDungNhanh_Game1.cs`, `MyMod/AutoDungNhanh_Game2.cs`, `MyMod/AutoDungNhanh/AutoDungNhanh.cs`.
- Không sửa đổi `AutoItem.cs`, `ModFunc.UseItemAuto()`, `ModFunc.listItemAuto`, `ModFunc.OpenMenu()` (phím `X`), phím `Q` (`case 113:`), nút cảm ứng `"QS"`, hay `AutoGop.cs`.

## 2. Cơ chế hoạt động đã triển khai
1. **Chọn nhanh trong Hành trang (`Panel.cs`):**
   - Tại `Game1/Panel.cs` (dòng `8864`) và `Game2/Panel.cs` (dòng `8859`), với vật phẩm không phải trang bị nằm trong hành trang (`!currItem.isTypeBody()` và thuộc hành trang, không áp dụng khi nâng cấp/giao dịch):
     - Hiện nút `"Auto\ndùng nhanh"` (hoặc `"Tắt auto\ndùng nhanh"` nếu đã thêm).
     - Ấn chọn sẽ **thêm/bật ngay lập tức** (hoặc gỡ/tắt nếu đã có) theo `delayMs` hiện tại **mà không hiện bảng hỏi nhập delay**.
2. **Phím `Z` mở Quick Menu giống `X` (`ModFunc.cs`):**
   - Tại `case 122:` (phím `Z`) trong `UpdateKey` (`Game1/ModFunc.cs` dòng `589`, `Game2/ModFunc.cs` dòng `589`) gọi `OpenQuickMenu()` dùng `GameCanvas.menu.startAt(myVector, 4)`.
   - Menu gồm 2 mục:
     - `"Auto nhanh"`: mở/đóng **Bảng Auto Dùng Nhanh** (đồng thời ẩn các bảng khác đang mở).
     - `"Bật auto\nnhanh"` / `"Tắt auto\nnhanh"`: bật/tạm dừng nhanh danh sách hiện có.
3. **Bảng Auto Dùng Nhanh & chỉnh Delay trực tiếp:**
   - Gọi vòng đời `Update()` (`Game1/ModFunc.cs:1319`, `Game2/ModFunc.cs:1314`) và `Paint(g)` (`Game1/ModFunc.cs:1618`, `Game2/ModFunc.cs:1613`).
   - Hỗ trợ chỉnh ô `Delay mỗi lần dùng` trực tiếp trên bảng (`delayMs` mặc định `200 ms`, giới hạn an toàn `50..60000 ms`), kéo cửa sổ, phân trang (4 dòng/trang), checkbox bật/tắt từng item, nút `[x]` xóa item, tùy chọn `Dừng đầy rương` (`stopOnBagFull`), `Xóa khi hết` (`autoRemoveWhenEmpty`), nút Bắt đầu/Dừng và Xóa danh sách item.
   - Cơ chế dùng item: xoay vòng (round-robin) các item đang bật, tìm đúng `indexUI` trong hành trang (`FindBagItemIndexUI`) và gọi `Service.gI().useItem(0, 1, (sbyte)bagIndexUI, -1)`.
4. **An toàn & tự dừng:**
   - `InfoMe.cs` (`Game1` dòng `323`, `Game2` dòng `323`) chuyển thông báo server vào `OnServerInfo(s)` để dừng khi hành trang đầy (`stopOnBagFull = true`).
   - `ServerListScreen.cs` (`Game1` dòng `752`, `Game2` dòng `752`) gọi `OnDisconnectOrReset()` để dừng auto và đóng cửa sổ khi mất kết nối / về màn hình đăng nhập (vẫn giữ cấu hình `delayMs` và danh sách).
5. **Cập nhật script sao lưu (`MyMod/do_backup.ps1`):**
   - Bổ sung `Game1.Mod\AutoDungNhanh.cs`, `Game2.Mod\AutoDungNhanh.cs`, `Game1\ServerListScreen.cs`, `Game2\ServerListScreen.cs` vào danh sách sao lưu.

## 3. Kết quả kiểm thử, biên dịch và sao lưu
- **Kiểm thử C# tự động (`AutoDungNhanhTests`):**
  - `Game1`: `8/8 PASS` (`0 Warning(s)`, `0 Error(s)`).
  - `Game2`: `8/8 PASS` (`0 Warning(s)`, `0 Error(s)`).
  - Các test bao gồm: Quick Menu phím `Z`, bật/tắt từ Hành trang không hỏi delay, sửa & kẹp biên Delay `[50..60000]` trên Bảng Auto Nhanh, đúng khoảng thời gian chờ và `indexUI` (test item ID `1581`), xoay vòng nhiều item / checkbox / nút `[x]`, phân trang 6 item & Xóa danh sách, hết đồ / đầy rương / reset kết nối, và đối soát `Game1`/`Game2`/`MyMod` + tách biệt code cũ.
- **Biên dịch mod (`MyMod/build_mod.ps1`):**
  - `0 Error(s)`, `0` cảnh báo liên quan đến `AutoDungNhanh`.
  - Xuất `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1,774,592` bytes, `11/13/2026 8:52:36 PM`).
- **Sao lưu snapshot (`MyMod/do_backup.ps1`):**
  - Tạo `Backup/Backup_Snapshot_20261113_205250.zip` (`2,065,480` bytes, `11/13/2026 8:53:09 PM`).

---

## Cập nhật [14/11/2026 02:33 (+07)] — Bổ sung ô `"Chạy liên tục"` trên giao diện Auto Dùng Nhanh
- **Trạng thái:** Đã đồng bộ `Game1`, `Game2`, `MyMod`, kiểm thử PASS 100%, biên dịch `Assembly-CSharp.dll` và sao lưu snapshot mới.
- **Chi tiết thay đổi (`AutoDungNhanh.cs`):**
  1. Thêm trường `continuousRun` (mặc định `false`) và checkbox **`"Chạy liên tục"`** trên giao diện bảng `AutoDungNhanh` (`winH = 264`, dòng tùy chọn thứ 2 `optRow2Y = winY + 69`, dời danh sách và nút đáy xuống `+16px` đồng bộ giữa `Paint` và `UpdateTouch`).
  2. Khi bật `"Chạy liên tục"`, tự động bỏ tick `"Xóa khi hết"` (`autoRemoveWhenEmpty = false`); nếu danh sách đang có `>= 1` vật phẩm đang bật (`HasEnabledItem()`), tự kích hoạt `isRunning = true` ngay lập tức.
  3. Khi vật phẩm trong hành trang tạm hết, `Update()` giữ nguyên `isRunning = true` ở trạng thái chờ và không xóa item khỏi danh sách; ngay khi vật phẩm xuất hiện lại trong hành trang sẽ tự động dùng tiếp theo `delayMs`.
  4. Quy tắc ưu tiên dừng an toàn:
     - **Đầy hành trang:** Nếu đang bật `"Dừng đầy rương"` và gặp thông báo đầy rương (`OnServerInfo`), vẫn ưu tiên dừng Auto và tự tắt `continuousRun = false`.
     - **Dừng chủ động:** Bấm nút `"■ DỪNG AUTO DÙNG NHANH"` trên bảng hoặc `Z -> Tắt auto nhanh` sẽ dừng ngay và tự tắt `continuousRun = false`.
     - **Bỏ tick hết các dòng item / xóa sạch danh sách:** Tạm dừng `isRunning = false`; nếu sau đó tick bật lại 1 item khi `continuousRun == true` sẽ tự chạy lại.
     - **Mất kết nối / về màn hình đăng nhập (`OnDisconnectOrReset`):** Tắt cả `isRunning = false` và `continuousRun = false` để tránh tự chạy nhầm khi đổi tài khoản.
- **Kiểm thử, build & backup sau cập nhật:**
  - **Kiểm thử C# (`AutoDungNhanhTests`):** `Game1` **9/9 PASS**, `Game2` **9/9 PASS** (`0 Warning(s)`, `0 Error(s)`), bổ sung ca kiểm thử `T8_ContinuousRun_Checkbox_WaitOnEmpty_ResumeAndStops`.
  - **Biên dịch (`MyMod/build_mod.ps1`):** `0 Error(s)`, `0` cảnh báo trong `AutoDungNhanh`, cập nhật `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1,775,616` bytes, `11/14/2026 2:33:07 AM`).
  - **Sao lưu (`MyMod/do_backup.ps1`):** `Backup/Backup_Snapshot_20261114_023324.zip` (`2,067,322` bytes, `11/14/2026 2:33:31 AM`).

