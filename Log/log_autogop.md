# Nhật ký cập nhật AutoGop (`log_autogop.md`)

- **Ngày thực hiện:** 2026-11-13 (GMT+7)
- **Phiên bản nền:** Khôi phục toàn bộ từ `Backup/Backup_Snapshot_20261112_144724` (nhánh Git `restore/144724`, build độc lập từ bản gốc `144724`, không trộn mã từ nhánh khác).
- **Phạm vi đồng bộ:** `Game1` (`DecompiledSource/Game1.Mod/AutoGop.cs`, `DecompiledSource/Game1/Controller.cs`), `Game2` (`DecompiledSource/Game2.Mod/AutoGop.cs`, `DecompiledSource/Game2/Controller.cs`), các bản mirror trong `MyMod/` (`AutoGop_Game1.cs`, `AutoGop_Game2.cs`, `AutoGop/AutoGop.cs`), và sửa đường dẫn sao lưu trong `MyMod/do_backup.ps1`.

---

## 1. Các thay đổi đã thực hiện

### 1.1. Thêm mục chọn số vòng lặp quy trình ở Tab Chung (`0 = vô hạn`)
- **Trạng thái & bộ đếm vòng lặp** (`DecompiledSource/Game1.Mod/AutoGop.cs` & `Game2.Mod/AutoGop.cs`):
  - Thêm `targetWorkflowLoops = 0` (`0 = vô hạn`), `completedWorkflowLoops`, `progressThisLoop`, `routeHopCount`.
  - Cập nhật `StartWorkflow()` (reset bộ đếm đầu quy trình) và tách `StartStandaloneTab(int tabId)` để các phím nóng `F4/F5/F7/F8` và nút chạy từng tab riêng chạy độc lập (`isRunningWorkflow = false`), không cộng nhầm vào số vòng quy trình.
  - Bổ sung `MarkLoopProgress()`, `FinishOneWorkflowLoop()`, `HandleLocalRoute(int targetTab, string message)` để:
    - Tự động chuyển sang vòng mới khi chạy tuần tự (`OnTabCompleted`) hoặc khi rẽ nhánh quay về tab đầu của quy trình (`targetTab == workflowQueue[0]`).
    - Dừng đúng khi `completedWorkflowLoops >= targetWorkflowLoops` (với `targetWorkflowLoops > 0`), hoặc tiếp tục lặp khi `targetWorkflowLoops == 0`.
    - Bảo vệ chống treo vô hạn: dừng an toàn nếu cả vòng không phát sinh giao dịch (`!progressThisLoop`), đích rẽ nhánh trùng tab hiện tại, hoặc rẽ nhánh lặp không phát sinh tiến triển quá 8 lần (`routeHopCount > 8`); đồng thời reset `routeHopCount = 0` mỗi khi phát sinh mua/đổi thành công qua `MarkLoopProgress()`.
- **Giao diện & nhập liệu Tab Chung (Tab 0)**:
  - Mở rộng chiều cao cửa sổ `winH` từ `246` lên `258` và dời các nút/ghi chú cuối cửa sổ cho cân đối (`winY + 181`, `startBtnY = winY + 206`, `stopBtnY = winY + 228`).
  - Vẽ dòng `"Vòng lặp (0=vô hạn):"` với ô hiển thị `"0=Vô hạn"` (khi `0`) hoặc số vòng đã đặt, kèm trạng thái `"Vòng X/Vô hạn"` hoặc `"Vòng X/Y"` khi đang chạy quy trình.
  - Thêm hitbox trong `UpdateTouch()` gọi `OpenInputField(6, ...)` và xử lý `case 6:` trong `onChatFromMe()` với `System.Math.Max(0, val)`.

### 1.2. Sửa lỗi nháy điểm giữa `10M` (`10.000.000`) và điểm hiện có ở Tab Điểm
- **Nguyên nhân gốc rễ:**
  - Tại `Controller.cs` (`case 32` trong cả `Game1` và `Game2`), mọi gói menu NPC đều gọi `ParseDiemPoints(text)` mà không truyền `npcId` và danh sách tùy chọn `menu`.
  - Khi Auto Điểm đang ở NPC 66 chọn mục `"Đổi điểm"`, server trả về Menu cấp 2 chứa các mốc đổi (`"10.000.000"`, `"5.000"`, `"3.000"`...). Regex trong `ParseDiemPoints` khớp nhầm số `10.000.000` (format `10M` qua `NinjaUtil.getMoneys`), ghi đè `currentDiemPoints = 10000000` cho tới khi vòng kế tiếp mở lại Menu cấp 1 cập nhật lại điểm thật, gây hiện tượng nháy liên tục giữa điểm thật và `10.000.000` (cũng làm sai điều kiện dừng Cách 2 `currentDiemPoints < RequiredPoints[pointIndex]`).
- **Cách khắc phục:**
  - Cập nhật `DecompiledSource/Game1/Controller.cs` (L3158, L3186) và `DecompiledSource/Game2/Controller.cs` (L3158, L3185) truyền đủ `(chatText, num177, array)` vào `ParseDiemPoints(string text, int npcId = 66, string[] menu = null)`.
  - Cập nhật `AutoGop.OnReceiveMenu` truyền `(chatText, npcTempId, menu)` khi gọi `ParseDiemPoints`.
  - Trong `ParseDiemPoints`:
    - Chỉ xử lý khi `npcId == 66`.
    - Kiểm tra danh sách `menu`: loại bỏ ngay các menu chứa mốc đổi Menu cấp 2 (`10.000.000`, `10,000,000`, `10tr`, `5.000`, `5,000`, `3.000`, `3,000`) và chỉ chấp nhận Menu cấp 1 có tùy chọn `"đổi điểm"` / `"doi diem"`.
    - Gỡ các tag định dạng màu/căn lề ở đầu dòng dạng `|color|` / `|color|align|`.
    - Ưu tiên cụm từ chỉ số dư của người chơi (`hiện có`, `đang có`, `số điểm`, `điểm hiện có`, v.v.) trước khi xét regex fallback, và chặn chuỗi fallback đa mốc (`10.000.000` đi cùng `5.000`/`3.000`).
    - Đổi kiểu `currentDiemPoints` từ `int` sang `long` để tránh tràn số khi điểm vượt `int.MaxValue`.
- **Sửa lỗi encoding bản `Game2` từ snapshot `144724`:**
  - File `DecompiledSource/Game2.Mod/AutoGop.cs` trong snapshot `144724` bị lỗi double-encoded UTF-8 mojibake. Đã tạo lại sạch UTF-8 trực tiếp từ `DecompiledSource/Game1.Mod/AutoGop.cs` (chỉ thay namespace/class `Game1` -> `Game2`) và đồng bộ đầy đủ sang `MyMod/AutoGop_Game1.cs`, `MyMod/AutoGop_Game2.cs`, `MyMod/AutoGop/AutoGop.cs`.

### 1.3. Chỉnh đường dẫn backup an toàn trong workspace
- Sửa `MyMod/do_backup.ps1`: dùng `$rootDir = Split-Path -Parent $PSScriptRoot` và `$backupDir = Join-Path $rootDir 'Backup'` (thay cho đường dẫn ngoài workspace trước đó), nén trực tiếp 4 thư mục backup vào `Backup/Backup_Snapshot_<timestamp>.zip`.

---

## 2. Kết quả kiểm thử (Testing Skill)

- **Kiểm tra tĩnh:** 5/5 PASS (`Game2 == Game1` khi đổi namespace, `MyMod` mirror đồng bộ 100%, không còn UTF-8 mojibake, 2 điểm gọi 3 tham số `ParseDiemPoints` trên cả `Game1/Controller.cs` và `Game2/Controller.cs`).
- **Kiểm thử mô phỏng C# độc lập** (18 test case phủ cả `parse điểm` không nháy `10M`, vòng lặp `0`/`1`/`2`/`3`, dừng khi vòng lặp không phát sinh giao dịch, rẽ nhánh `Zeno <-> HN` cả trường hợp hiệu quả và không hiệu quả, chống tự rẽ nhánh, phím nóng độc lập và Touch UI + nhập liệu `onChatFromMe`):
  - Target `Game1` (`DecompiledSource/Game1.Mod/AutoGop.cs`): **18/18 PASSED, 0 FAILED** (`0 Warning(s), 0 Error(s)`).
  - Target `Game2` (`DecompiledSource/Game2.Mod/AutoGop.cs`): **18/18 PASSED, 0 FAILED** (`0 Warning(s), 0 Error(s)`).

---

## 3. Kết quả Build & Sao lưu

- **Build mod (`MyMod/build_mod.ps1`):** Thành công (`0 Error(s)`, không có warning nào trong `AutoGop.cs` hoặc `Controller.cs`), đã cập nhật `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1,758,208` bytes).
- **Sao lưu snapshot (`MyMod/do_backup.ps1`):** Thành công, đã tạo `Backup/Backup_Snapshot_20261113_173523.zip` (`2,022,299` bytes).

---

## Cập nhật [14/11/2026 03:03 (+07)] — Đổi giá trị mặc định các Tab trong `AutoGop`
- **Phạm vi:** Chỉ thay đổi giá trị khởi tạo mặc định và chuỗi gợi ý (`OpenInputField` placeholder) tương ứng trong `DecompiledSource/Game1.Mod/AutoGop.cs`, `DecompiledSource/Game2.Mod/AutoGop.cs` và các bản mirror `MyMod/AutoGop_Game1.cs`, `MyMod/AutoGop_Game2.cs`, `MyMod/AutoGop/AutoGop.cs`. **Không thay đổi bất kỳ logic xử lý nào.**
- **Các giá trị mặc định đã đổi:**
  - **Tab Bánh:**
    - `delayBuyBanhMs`: `200` -> `600` (giữ nguyên `delayUseBanhMs = 200`, placeholder `"600"` tại L1492).
    - `stopConditionBanh`: `1` -> `2` (chọn sẵn Điều kiện 2: đạt mốc rương).
    - `targetItemCountBanh`: `5000` -> `2000000` (placeholder `"2000000"` tại L1522).
  - **Tab HN:**
    - `targetBatchesHn`: `50` -> `0` (`0 = vô hạn`, placeholder `"0"` tại L1551).
    - `delayBuyHnMs`: `200` -> `600` (placeholder `"600"` tại L1559).
  - **Tab Điểm:**
    - `pointIndex`: `1` (`5K`) -> `0` (`10Tr` / `10M`).
    - `targetBatchesDiem`: `50` -> `1` (placeholder `"1"` tại L1606).
- **Kiểm thử & Biên dịch sau cập nhật:**
  - **Kiểm thử C# (`AutoGop144724Tests`, bổ sung ca `T19_DefaultValues_And_Placeholders`):**
    - `Game1`: **19/19 PASSED, 0 FAILED** (`0 Warning(s), 0 Error(s)`).
    - `Game2`: **19/19 PASSED, 0 FAILED** (`0 Warning(s), 0 Error(s)`).
  - **Build mod (`MyMod/build_mod.ps1`):** `0 Error(s)`, `0` cảnh báo trong `AutoGop.cs`, cập nhật `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1,775,616` bytes, `11/14/2026 3:02:56 AM`).
  - **Ghi chú:** Không chạy `do_backup.ps1` và giữ nguyên `Log/log_version.md` theo yêu cầu hiện tại.


---

## Cập nhật [14/11/2026 03:37 (+07)] — Bổ sung delay `500ms` & ô chỉnh sửa khi rẽ nhánh `HN -> Zeno` và `Zeno -> Bánh/HN` trong `AutoGop`
- **Phạm vi:** Chỉ bổ sung độ trễ chuyển tab cục bộ và hai ô nhập liệu tương ứng trên Tab HN & Tab Zeno trong `DecompiledSource/Game1.Mod/AutoGop.cs`, `DecompiledSource/Game2.Mod/AutoGop.cs` cùng các bản mirror `MyMod/AutoGop_Game1.cs`, `MyMod/AutoGop_Game2.cs`, `MyMod/AutoGop/AutoGop.cs`. Không thay đổi kích thước cửa sổ (`winW = 224, winH = 258`), không sửa `FinishOneWorkflowLoop()` (quay lại `workflowQueue[0]` vẫn dùng `delayBetweenTabsMs`), và không đụng các logic gốc khác.
- **Chi tiết thay đổi:**
  - **Tab HN (Tab 2):**
    - Thêm `routeDelayHnMs = 500;` (mặc định `500 ms`).
    - Trong `Paint()` & `UpdateTouch()`: thêm dòng `"Delay chuyển Zeno:"` kèm ô nhập hiển thị `routeDelayHnMs + " ms"` (gọi `OpenInputField(25, "Delay chuyển tab HN -> Zeno (ms)", "500")`, nhập tối thiểu `50 ms` qua `case 25` trong `onChatFromMe`).
  - **Tab Zeno (Tab 4):**
    - Thêm `routeDelayZenoMs = 500;` (mặc định `500 ms`).
    - Trong `Paint()` & `UpdateTouch()`: thêm dòng `"Delay chuyển tab:"` kèm ô nhập hiển thị `routeDelayZenoMs + " ms"` (gọi `OpenInputField(46, "Delay chuyển tab Zeno -> Bánh/HN (ms)", "500")`, nhập tối thiểu `50 ms` qua `case 46` trong `onChatFromMe`).
  - **Máy trạng thái rẽ nhánh cục bộ:**
    - Bổ sung `stepState = 7` (`LocalRouteDelay`), `pendingRouteTab`, `pendingRouteDelayMs` và `ScheduleLocalRoute(int targetTab, int sourceTab)` để chờ đúng `routeDelayHnMs` (khi từ HN) hoặc `routeDelayZenoMs` (khi từ Zeno Nhánh 1 / Nhánh 2) trước khi gọi `StartTab(nextTab)`.
    - Khi ở `stepState = 7`, đặt `currentRunningTab = 0` để gói tin trễ từ tab cũ không kích hoạt rẽ nhánh lại; `Update()` xử lý chờ ở `if (stepState == 7)` và `StopAll(...)` reset sạch `pendingRouteTab = 0; pendingRouteDelayMs = 0;`.
- **Kiểm thử & Biên dịch sau cập nhật:**
  - **Kiểm thử C# (`AutoGop144724Tests`, bổ sung `T20_LocalRouteDelay_500ms_And_EditableBoxes`):**
    - `Game1`: **20/20 PASSED, 0 FAILED** (`0 Warning(s), 0 Error(s)`).
    - `Game2`: **20/20 PASSED, 0 FAILED** (`0 Warning(s), 0 Error(s)`).
  - **Build mod (`MyMod/build_mod.ps1`):** `0 Error(s)`, `0` cảnh báo trong `AutoGop.cs` (`246 Warning(s)` sẵn có của mã gốc), cập nhật `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (`1,777,152` bytes, `2026-11-14 03:36:58`).
  - **Ghi chú:** Không chạy `do_backup.ps1` và không chỉnh sửa `Log/log_version.md` theo yêu cầu.
