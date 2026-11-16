# Kiến Trúc & Luồng Hoạt Động Toàn Bộ Mod (`Nro_GOC`)

> Tài liệu tham chiếu kỹ thuật dành cho lập trình viên tiếp theo: mô tả kiến trúc build, bản đồ các module mod đang hoạt động, luồng dữ liệu / gói tin server, bảng phím tắt - lệnh chat tránh xung đột, và quy trình từng bước để thêm chức năng mới an toàn.

---

## 1. Tổng Quan Kiến Trúc & Cách Build Mod

### 1.1. Cơ chế biên dịch thực tế
- **Toàn bộ mod được biên dịch trực tiếp vào DLL của game** qua project SDK-style [Assembly-CSharp.csproj](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Assembly-CSharp.csproj) (`netstandard2.1`, C# 11).
- Script build: [build_mod.ps1](file:///c:/Users/admin/Documents/Nro_GOC/MyMod/build_mod.ps1) gọi `dotnet build` ở cấu hình `Release` và copy kết quả sang `ProjCompleted_Data/Managed/Assembly-CSharp.dll`.
- Lưu ý: Thư mục [MyMod/](file:///c:/Users/admin/Documents/Nro_GOC/MyMod) có sẵn `Plugin.cs` / `MyMod.csproj` (khung BepInEx/Harmony mẫu), **không phải** nơi mod hiện tại chạy; các file `*_Game1.cs`, `*_Game2.cs` trong `MyMod/` là bản sao đồng bộ/lưu trữ của source trong `DecompiledSource/`.

### 1.2. Cấu trúc 2 Client song song (`Game1` và `Game2`)
Source game chứa 2 bản song song trong cùng `Assembly-CSharp.dll`:
- `DecompiledSource/Game1/`, `DecompiledSource/Game1.Mod/`, `DecompiledSource/Game1.Mod.XMAP/`, `DecompiledSource/Game1.Assets.src.f/`
- `DecompiledSource/Game2/`, `DecompiledSource/Game2.Mod/`, `DecompiledSource/Game2.Mod.XMAP/`, `DecompiledSource/Game2.Assets.src.f/`

**Quy tắc bắt buộc:** Mọi module mới hoặc sửa đổi hook đều phải làm đồng thời cho cả **`Game1`** và **`Game2`** (chỉ khác namespace `Game1.*` ↔ `Game2.*`), đồng bộ bản sao vào `MyMod/`, và bổ sung đường dẫn vào mảng `$files` trong [do_backup.ps1](file:///c:/Users/admin/Documents/Nro_GOC/MyMod/do_backup.ps1).

---

## 2. Vòng Đời Thực Thi & Điểm Tích Hợp (Hook Pipeline)

Mỗi frame và mỗi gói tin server đi qua các điểm nối cố định sau (số dòng tham chiếu tại `Game1`, `Game2` có cấu trúc tương ứng):

| Điểm Hook trong Game Gốc | Vị trí (Game1) | Thứ tự / Vai trò |
| :--- | :--- | :--- |
| **Game Loop (`Update`)** | [GameScr.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/GameScr.cs#L4330-L4336) -> [ModFunc.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/ModFunc.cs#L1318-L1328) | `AutoXmap.Update()` -> `ModFunc.UpdateTouch()` -> `AutoItem.Update()` -> `AutoMuaNhieu` -> `AutoGop` -> `NpcMenuHelper` -> `AutoDungNhanh` -> `AutoGiaoDich` -> `ShowBoss.UpdateNotifications()` -> các auto nền trong `ModFunc` (`UseItemAuto`, buff đệ, auto chat, VQMM, dò boss...). |
| **Vẽ UI (`Paint`)** | [GameScr.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/GameScr.cs#L4972) -> [ModFunc.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/ModFunc.cs#L1622-L1627) | Vẽ cửa sổ nổi & HUD của `AutoMuaNhieu`, `AutoGop`, `NpcMenuHelper`, `AutoDungNhanh`, `AutoGiaoDich`, và thông báo Boss / người chơi trong map. |
| **Phím tắt (`UpdateKey`)** | [GameScr.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/GameScr.cs#L2628) -> [ModFunc.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/ModFunc.cs#L506-L603) | Bắt phím ASCII (`X`, `Z`, `A`, `G`, `Q`, v.v.). Ngoài ra `AutoGop` và `AutoGiaoDich` nghe thêm `UnityEngine.Input.GetKeyDown` (`F3..F8`, `F6`) trong hàm `Update()`. |
| **Lệnh Chat (`Chat`)** | [GameScr.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/GameScr.cs#L6248) -> [ModFunc.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/ModFunc.cs#L1090-L1217) | Chuyển chuỗi chat lần lượt cho `AutoGop.HandleChatCommand` -> `AutoMuaNhieu.HandleChatCommand` -> `NpcMenuHelper.HandleChatCommand` -> các lệnh chat có sẵn trong `ModFunc`. |
| **Gói tin chính (`Controller`)** | [Controller.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/Controller.cs) | `case -86:` (giao dịch -> `AutoGiaoDich`); `case -36:` (cập nhật rương -> `AutoGiaoDich.OnBagUpdated`); `case -44:` (mở shop -> `AutoGop.OnShopOpened`, ẩn Panel nếu `AutoGop.hideUi`); `case 32:` (menu NPC -> `AutoGop.ParseDiemPoints` + ẩn `ChatPopup` nếu `AutoGop`/`NpcMenuHelper` bật `hideUi`); hook Xmap (`ShowPanelMapTrans`, `FixBlackScreen`). |
| **Gói Input Server (`-125`), Chat VIP (`93`) & Tích Xanh (`98`)** | [Controller2.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Assets.src.f/Controller2.cs#L38-L51) | `case -125:` đẩy qua `AutoGop` -> `AutoMuaNhieu` -> `NpcMenuHelper` trước khi hiện `ClientInput`; `case 93:` qua `GameScr.chatVip` (`ShowBoss.HandleChatVip` tại [GameScr.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/GameScr.cs#L6808)); `case 98:` gọi `ModFunc.AddNotifTichXanh`. |
| **Menu NPC (`createMenu`)** | [GameScr.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/GameScr.cs#L1275-L1291) | Khi có menu lựa chọn NPC: `AutoGop.OnReceiveMenu` -> `NpcMenuHelper.OnReceiveMenu` có thể chặn hiện menu (`hideUi`) và gửi chọn tự động. |
| **Gửi lựa chọn NPC (`confirmMenu`)** | [Service.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/Service.cs#L1037) | Gọi `NpcMenuHelper.gI().OnConfirmMenu(npcID, select)` để ghi log Spy. |
| **Hành trang / Shop (`Panel.cs`)** | [Panel.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/Panel.cs#L8573) | Hành trang thêm nút `"Auto\ndùng nhanh"` (`L8864`); Shop focus/mua item đẩy sang `AutoMuaNhieu` / `AutoGop` (`L8573`, `L10359-10418`); Xmap map chuyển (`L4384`). |
| **Thông báo server (`InfoMe`)** | [InfoMe.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/InfoMe.cs#L316-L325) | Phân phát chuỗi thông báo `s` tới `AutoXmap.Info`, `AutoMuaNhieu`, `AutoGop`, `NpcMenuHelper`, `AutoDungNhanh`, `AutoGiaoDich`. |
| **Reset / Đăng xuất** | [ServerListScreen.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/ServerListScreen.cs#L750-L755) | Gọi `AutoDungNhanh.gI().OnDisconnectOrReset()`, `AutoGiaoDich.gI().OnDisconnectOrReset()`, tắt `ModFunc.startAutoItem`. |

---

## 3. Chi Tiết Từng Mod & Luồng Hoạt Động

### 3.1. `AutoGiaoDich` — Tự Động Giao Dịch 2 Chiều (`Z -> GD nhanh`, `F6` dừng)
- **File:** [Game1.Mod/AutoGiaoDich.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Mod/AutoGiaoDich.cs), [Game2.Mod/AutoGiaoDich.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2.Mod/AutoGiaoDich.cs), log kỹ thuật: [log_autogiaodich.md](file:///c:/Users/admin/Documents/Nro_GOC/Log/log_autogiaodich.md).
- **Chức năng:** Tự động thực hiện giao dịch lặp lại nhiều lượt giữa 2 acc cùng chạy mod (**Người GỬI** và **Người NHẬN**). Cấu hình trực tiếp trên cửa sổ nổi (`Z -> GD nhanh`):
  - Chọn vai trò (`Sender` / `Receiver`), chọn đối tác từ danh sách cùng map (`GameScr.vCharInMap`) hoặc `[Target]` (`charFocus`), chọn vật phẩm từ rương hoặc nhập `ID`.
  - Chỉnh `SL/lượt` (`qtyPerTrade`), `Số lượt` (`targetLoops`, `0 = Vô hạn`), 3 mức delay (`Delay GD` giữa 2 lượt, `Delay khóa`, `Delay XN` xác nhận), và checkbox `Ẩn Panel` (`hidePanelDuringTrade`, mặc định bật).
- **Luồng trạng thái (`TradeState`):**
  - **Bên Gửi (`Sender`):**
    1. Kiểm tra `GetBagItemCount(targetItemId) >= qtyPerTrade`, `targetLoops`, đối tác còn trong map -> gửi `giaodich(0, targetCharId, -1, -1)` (`SenderSendInvite` -> `WaitOpenSession`, tối đa `MaxInviteRetries = 8`).
    2. Nhận `-86` `b13 == 1` (`OnTradeOpened`) -> sang `PutItems`: tách/gom qua nhiều ô `arrItemBag` cho đủ `qtyPerTrade` và gửi `giaodich(2, -1, indexUI, putQty)`.
    3. Sang `WaitLockDelay`: đợi `delayLockMs` rồi gửi `giaodich(5, -1, -1, -1)`. (Nếu `b13 == 2` / `OnMyItemRemoved` đến trước khi khóa -> hủy & dừng ngay).
    4. Nếu `b13 == 6` (`OnFriendLocked`) đã tới trước -> vào thẳng `WaitAcceptDelay`; nếu chưa -> đợi ở `WaitPartnerLock`.
    5. `WaitAcceptDelay`: đợi `delayAcceptMs` -> đặt `sentAccept = true`, gửi `giaodich(7, -1, -1, -1)` -> `WaitTradeClose`.
    6. Nhận `-86` `b13 == 7` (`OnTradeClosed`): tăng `completedTrades`, sang `WaitCooldownNextTrade` đợi `delayBetweenTradesMs` + cờ `bagUpdatedSinceTradeClose` từ `case -36:` (`OnBagUpdated`, tối đa 200ms nếu delay = 0) trước khi kiểm tra lại `bag < qtyPerTrade` để mời lượt kế tiếp hoặc dừng.
  - **Bên Nhận (`Receiver`):**
    1. Đứng ở `ReceiverWaitingInvite`. Khi `-86` `b13 == 0` đến (`OnReceiveInvite`): **chỉ nhận** nếu `playerID == targetCharId`, chặn `PopUpYesNo` và gửi `giaodich(1, playerID, -1, -1)`.
    2. Khi `b13 == 1` (`OnTradeOpened`) -> vào thẳng `WaitLockDelay` -> đợi `delayLockMs` rồi khóa (`giaodich(5)`) -> đợi bên gửi khóa (`b13 == 6`) -> đợi `delayAcceptMs` -> xác nhận (`giaodich(7)`) -> quay lại `ReceiverWaitingInvite`.
- **An toàn UI & Dừng:** Khi `hidePanelDuringTrade = true`, `Controller.cs` bỏ qua `panel.setTypeGiaoDich`/`panel.show()` (`b13 == 1`), `panel.hide()` (`b13 == 7`), và không ghi đè UI ở `b13 == 6` (nhưng **vẫn đọc hết byte của message**). Phím **`F6`** (hoặc lỗi server/rời map) dừng auto và gửi `giaodich(3, -1, -1, -1)` nếu phiên đang mở dở.

### 3.2. `AutoDungNhanh` — Dùng Nhanh Vật Phẩm (`Z -> Auto nhanh / Bật-Tắt auto nhanh`)
- **File:** [Game1.Mod/AutoDungNhanh.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Mod/AutoDungNhanh.cs), [Game2.Mod/AutoDungNhanh.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2.Mod/AutoDungNhanh.cs), log kỹ thuật: [log_autodungnhanh.md](file:///c:/Users/admin/Documents/Nro_GOC/Log/log_autodungnhanh.md).
- **Chức năng:** Module dùng vật phẩm tốc độ cao theo miligiây (`delayMs` mặc định `200ms`, kẹp `[50..60000ms]`), tách biệt hoàn toàn với `AutoItem.cs` (dùng theo giây).
- **Luồng hoạt động:**
  1. Người chơi vào Hành trang (`Panel.cs:8864`) bấm `"Auto\ndùng nhanh"` để thêm/bật ngay item không phải trang bị vào danh sách (`items`), hoặc bấm **`Z`** để mở Quick Menu (`Auto nhanh` mở cửa sổ quản lý; `Bật/Tắt auto nhanh` chạy/dừng).
  2. `Update()` xoay vòng round-robin qua các item đang bật (`entry.enabled`), tìm `indexUI` trong `arrItemBag` (`FindBagItemIndexUI`) và gọi `Service.gI().useItem(0, 1, (sbyte)bagIndexUI, -1)`.
  3. Tùy chọn trên bảng: `Chạy liên tục` (`continuousRun` — giữ trạng thái chờ nếu tạm hết đồ và bỏ tick `Xóa khi hết`), `Dừng đầy rương` (`stopOnBagFull` qua `OnServerInfo`), `Xóa khi hết` (`autoRemoveWhenEmpty`). Tự tắt an toàn khi mất kết nối (`OnDisconnectOrReset`).

### 3.3. `AutoGop` — Quy Trình Tự Động 5 Tab (`F3`..`F8`, Menu `X -> Auto Gộp`)
- **File:** [Game1.Mod/AutoGop.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Mod/AutoGop.cs), [Game2.Mod/AutoGop.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2.Mod/AutoGop.cs), tài liệu tab chi tiết: [data.md](file:///c:/Users/admin/Documents/Nro_GOC/data.md), log kỹ thuật: [log_autogop.md](file:///c:/Users/admin/Documents/Nro_GOC/Log/log_autogop.md).
- **Chức năng:** Điều phối chuỗi mua/đổi qua 5 tab: **Tab 0 (Chung)**, **Tab 1 (Bánh - NPC `92`, Item `1337`)**, **Tab 2 (HN - NPC `56`, Item `1581`)**, **Tab 3 (Điểm - NPC `66`)**, **Tab 4 (Zeno - NPC `18`, Item vé `1583`)**.
- **Luồng hoạt động (`stepState 0..7`):**
  1. Khởi động bằng **`F3`** (chạy quy trình `StartWorkflow` theo thứ tự `orderBanh/HN/Diem/Zeno` và lặp `targetWorkflowLoops`), hoặc **`F4/F5/F7/F8`** (chạy lẻ từng tab qua `StartStandaloneTab`), hoặc lệnh chat `gop` / `f3` / `f6`. Dừng khẩn cấp bằng **`F6`** hoặc phím **`A`**.
  2. Tới gần NPC (`MoveCloseToNpc`), tự chat `"muanhieu"` nếu cần (`autoChatMuaNhieu`), mở menu NPC -> `OnReceiveMenu` tự chọn nhánh shop/đổi -> `OnShopOpened` kích hoạt `buyItem` -> bắt gói `-125` (`OnReceiveServerInputPacket` trong `Controller2`) bắn ngầm số lượng cần mua/đổi.
  3. Hỗ trợ **rẽ nhánh cục bộ** (`stepState = 7`, `LocalRouteDelay`): ví dụ từ `HN` hết tài nguyên rẽ sang `Zeno` (mặc định `routeDelayHnMs = 500ms`), hoặc từ `Zeno` đủ mục tiêu rẽ sang `HN` / thiếu vé rẽ sang `Bánh` (`routeDelayZenoMs = 500ms`). Chế độ `hideUi` ẩn hộp thoại NPC, Shop và `ClientInput`.
  4. `ParseDiemPoints(text, npcId, menu)` chỉ phân tích menu cấp 1 của NPC `66` (lọc bỏ menu cấp 2 chứa mốc `10.000.000` để không nháy điểm).

### 3.4. `AutoMuaNhieu` — Mua Shop Số Lượng Lớn (`Menu X -> Auto Mua Nhiều`)
- **File:** [Game1.Mod/AutoMuaNhieu.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Mod/AutoMuaNhieu.cs), [Game2.Mod/AutoMuaNhieu.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2.Mod/AutoMuaNhieu.cs).
- **Chức năng:** Tự động mua liên tiếp 1 vật phẩm trong Shop có hỗ trợ lệnh `"muanhieu"` của server.
- **Luồng hoạt động (`state 0..4`):**
  1. Chọn item khi bấm/focus trong Shop (`Panel.cs:8573`, `10359-10397`) hoặc lệnh chat `amn`, `amn id <id>`, `amn sl <1..1000>`, `amn lap <n>`, `amn delay <ms>`.
  2. Khi chạy: gửi chat `"muanhieu"` nếu chưa bật (`state 1`) -> gửi `Service.gI().buyItem(targetBuyType, targetItemId, 0)` (`state 2`) -> đợi gói `-125` (`state 3` -> `OnReceiveServerInputPacket` ở `Controller2` tự gửi `sendClientInput(batchQuantity)`) -> nghỉ `delayMs` (`state 4`) -> lặp đến khi đủ `targetBatches` hoặc `OnServerInfo` báo đầy/thiếu hoặc nhấn phím **`A`**.

### 3.5. `NpcMenuHelper` — Menu Spy & Auto Chọn Menu NPC
- **File:** [Game1.Mod/NpcMenuHelper.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Mod/NpcMenuHelper.cs), [Game2.Mod/NpcMenuHelper.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2.Mod/NpcMenuHelper.cs).
- **Chức năng:**
  - **Menu Spy:** Ghi lại cấu trúc menu NPC và lựa chọn gửi lên (`OnConfirmMenu` ở `Service.cs:1037`) ra log (`menu_spy.txt`) qua lệnh chat `spy`.
  - **Auto Menu:** Tự động mở menu NPC và chọn chuỗi bước định sẵn (`autoType`: `0` Custom steps qua lệnh `auto <npcId> <bước1> <bước2>... [delay_ms]`, `1` Đổi điểm Nồi Bánh NPC `66` qua `autobanh`/`doidiem`, `2` Đổi Tiên Phù Zeno NPC `18` qua `autozeno`/`doizeno`/`tienphu`). Tự bắt gói `-125` cho NPC `18` và ẩn UI nếu `hideUi = true`. Dừng bằng phím **`A`** hoặc lệnh `stop`/`dung`.

### 3.6. `ModFunc` — Trục Điều Phối Trung Tâm & Các Tiện Ích Gốc
- **File:** [Game1/ModFunc.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/ModFunc.cs), [Game2/ModFunc.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2/ModFunc.cs).
- **Chức năng chính tích hợp sẵn:**
  - **Menu `X` (`OpenMenu`):** Chứa Bản đồ (`XmapController`), Luyện tập (`PickMob`), Nhặt đồ, Phím tắt `Q` (`quickItemId`), Đệ tử (buff đậu), BOSS, `Auto Mua Nhiều`, `Auto Gộp`, Khác (FPS, tốc độ game, giảm đồ họa, load skill).
  - **Các auto chạy nền trong `ModFunc.Update()`:** Giữ kết nối (keepalive 5 phút), tự buff đậu cho đệ (`isPeanPet`), tự quay VQMM (`isAutoVQMM`), tự pha lê (`isAutoPhaLe`), tự chat / chat thế giới (`isAutoChat`, `isAutoChatTG`), tự dùng item buff theo `iconID` (`UseItemAuto`), cập nhật khu (`isUpdateZones`), tự dò boss / săn boss (`ShowBoss`, `lineToBoss`, `focusBoss`), quản lý tài khoản & các nút cảm ứng kéo thả (`buttonPositions`).

### 3.7. `AutoItem` — Auto Dùng Vật Phẩm Cơ Bản (Theo Giây)
- **File:** [Game1/AutoItem.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1/AutoItem.cs), [Game2/AutoItem.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2/AutoItem.cs).
- **Chức năng:** Thêm/xóa item từ Hành trang (`ModFunc` action `500/501`), nhập số **giây** giãn cách, kiểm tra mỗi `20 tick` (`GameCanvas.gameTick % 20 != 0`) nếu `currentTimeMillis() - lastTimeUseItem > timeDelay * 1000` thì gọi `useItem`. Ngoài ra cung cấp `AutoItem.ResetTF(ChatTextField.gI())` dùng chung để dọn `ChatTextField` cho các mod khác.

### 3.8. Hệ thống `XMAP` & `PickMob` (Di Chuyển & Tàn Sát / Nhặt Đồ)
- **Thư mục:** `DecompiledSource/Game1.Mod.XMAP/` và `DecompiledSource/Game2.Mod.XMAP/`.
- **Xmap (`AutoXmap`, `XmapController`, `XmapData`, `XmapAlgorithm`, `ManualXmap`):** Tìm đường ngắn nhất giữa các map (`WayXmap`), tự chuyển map qua waypoint/NPC/Capsule, hỗ trợ click bản đồ (`SelectMapTrans`) và sửa màn hình đen (`FixBlackScreen`), tự dừng khi `InfoMe` báo chưa vào được khu vực hoặc bị hủy. `ManualXmap` hỗ trợ phím `J / K / L` đi cửa trái/giữa/phải.
- **PickMob (`PickMob`, `PickMobController`):** Chạy sau `ModFunc.Update()` (khi không Xmap ở `GameScr.cs:4335`): tự nhặt vật phẩm (`IsAutoPickItems`, `IsPickItemsDis`, `IsPickItemsAll`, lọc ID/loại), tàn sát quái/người (`tanSat`, `tsPlayer`, `neSieuQuai`, `vuotDiaHinh`, `telePem`), tự hồi HP/MP, và `GoBack` quay lại tọa độ/map/khu cũ khi hồi sinh.

### 3.9. `ShowBoss` — Thông Báo & Đi Đến Boss
- **File:** [Game1.Mod/ShowBoss.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game1.Mod/ShowBoss.cs), [Game2.Mod/ShowBoss.cs](file:///c:/Users/admin/Documents/Nro_GOC/DecompiledSource/Game2.Mod/ShowBoss.cs).
- **Chức năng:** Nhận dòng chat VIP (`HandleChatVip` gọi từ `Controller2`) có từ khóa `"boss"` + `"xuất hiện"` hoặc `"tiêu diệt"`, phân tích tên boss và `mapID` (`ModFunc.GetMapID`), hiển thị góc phải màn hình (nhấp chuột vào dòng boss để gọi `ModFunc.GoToBoss(mapID)`).

---

## 4. Bảng Tra Cứu Phím Tắt & Lệnh Chat (Tránh Trùng Khi Mở Rộng)

### 4.1. Phím tắt đang chiếm dụng
| Phím | Chức năng hiện tại | Nơi xử lý |
| :--- | :--- | :--- |
| **`A`** (`97`) | Dừng `AutoGop` / `AutoMuaNhieu` / `NpcMenuHelper` nếu đang chạy; không thì dịch trái 100px | `ModFunc.UpdateKey`, `AutoGop/AutoMuaNhieu/NpcMenuHelper.Update` |
| **`B`** (`98`) | Chat `"boss"` | `ModFunc.UpdateKey` |
| **`C`** (`99`) | Dùng item ID `194` (Capsule) | `ModFunc.UpdateKey` |
| **`D`** (`100`) | Dịch phải 100px | `ModFunc.UpdateKey` |
| **`E`** (`101`) | Mở danh sách bạn bè | `ModFunc.UpdateKey` |
| **`F`** (`102`) | Dùng Bông tai Porata (`UsePorata`) | `ModFunc.UpdateKey` |
| **`G`** (`103`) | Gửi mời giao dịch tới `charFocus` | `ModFunc.UpdateKey` |
| **`H`** (`104`) | Bật/tắt `"ukhu"` (cập nhật khu) | `ModFunc.UpdateKey` |
| **`J / K / L`** (`106..108`) | `ManualXmap` chuyển map Trái / Giữa / Phải | `ModFunc.UpdateKey` |
| **`M`** (`109`) | Mở bảng chọn khu | `ModFunc.UpdateKey` |
| **`N`** (`110`) | Bật/tắt tự nhặt (`PickMob.IsAutoPickItems`) | `ModFunc.UpdateKey` |
| **`Q`** (`113`) | Dùng nhanh vật phẩm phím tắt (`UseQuickItem`) | `ModFunc.UpdateKey` |
| **`S`** (`115`) | Dịch xuống 100px | `ModFunc.UpdateKey` |
| **`T`** (`116`) | Dùng item ID `521` (TDLT) | `ModFunc.UpdateKey` |
| **`U`** (`117`) | Bật/tắt Tự đánh (`autoAttack`) | `ModFunc.UpdateKey` |
| **`W`** (`119`) | Dịch lên 100px | `ModFunc.UpdateKey` |
| **`X`** (`120`) | Mở Menu Mod chính (`ModFunc.OpenMenu`) | `ModFunc.UpdateKey` |
| **`Z`** (`122`) | Mở Quick Menu (`Auto nhanh`, `Bật/Tắt auto nhanh`, `GD nhanh`) | `ModFunc.UpdateKey` |
| **`F3 / F4 / F5 / F7 / F8`** | Chạy `AutoGop` (Quy trình / Bánh / HN / Điểm / Zeno) | `AutoGop.Update` |
| **`F6`** | Dừng `AutoGop` và dừng `AutoGiaoDich` | `AutoGop.Update`, `AutoGiaoDich.Update` |

### 4.2. Lệnh chat đang chiếm dụng (`ModFunc.Chat`)
- **AutoGop:** `gop`, `autogop`, `f3`, `chaygop`, `f6`, `dunggop`
- **AutoMuaNhieu:** `amn`, `automua`, `muanhieu`, `amn id <n>`, `amn sl <n>`, `amn lap <n>`, `amn delay <n>`, `npc`, `idnpc`, `dsnpc`
- **NpcMenuHelper:** `spy`, `logmenu`, `anui`, `hideui`, `hienui`, `showui`, `stop`, `dung`, `dungmenu`, `autobanh ...`, `doidiem ...`, `autozeno ...`, `doizeno ...`, `tienphu ...`, `auto <npcId> <steps> [delay]`
- **ModFunc gốc:** `bpa`, `loadskill`, `ak`, `ts`, `tsnguoi`, `vqmm`, `ukhu`, `anhat`, `nhat`, `nhatxa`, `nhatall`, `qs ...`, `citem ...`, `item ...`, `k <khu>`, `s <speed>`, `atc <msg>`, `atctg <msg>`, `do <boss>`, `dbx`, `gtv`...

---

## 5. Hướng Dẫn Thêm Chức Năng Mod Mới (Developer Playbook)

Khi người dùng yêu cầu làm thêm chức năng mới, hãy thực hiện chuẩn theo 6 bước sau để **không làm hỏng logic gốc** và vượt qua mọi bẫy của codebase này:

### Bước 1: Tách file riêng trong `Game1.Mod` và `Game2.Mod`
- Tạo class Singleton mới: `DecompiledSource/Game1.Mod/<TenMod>.cs` (namespace `Game1.Mod;`) và bản gương `DecompiledSource/Game2.Mod/<TenMod>.cs` (`namespace Game2.Mod;`).
- Hạn chế tối đa sửa trực tiếp logic dài trong `Panel.cs`, `GameScr.cs`, `Service.cs`, hay các mod cũ (`AutoGop.cs`, `AutoDungNhanh.cs`, v.v.) trừ khi có yêu cầu cụ thể gắn nút vào đó.
- Đồng bộ thêm 2 bản lưu sang `MyMod/<TenMod>_Game1.cs` và `MyMod/<TenMod>_Game2.cs`.

### Bước 2: Gắn hook tối thiểu vào luồng game
1. **Chạy & vẽ UI:** Thêm `Update()` và `Paint(mGraphics g)` vào `ModFunc.Update()` và `ModFunc.Paint()` ở cả `Game1/ModFunc.cs` và `Game2/ModFunc.cs`. Nếu UI cho nhập số qua `ChatTextField`, nhớ bỏ qua phím nóng khi `ChatTextField.gI().isShow == true`.
2. **Điểm mở UI:**
   - Gắn vào Menu `Z` (`case 122:` trong `ModFunc.UpdateKey`), hoặc Menu `X` (`ModFunc.OpenMenu` + xử lý `perform`), hoặc lệnh chat (`HandleChatCommand` đặt ở đầu `ModFunc.Chat`).
   - Khi mở cửa sổ mới, đóng các cửa sổ mod khác (`AutoGop`, `AutoMuaNhieu`, `NpcMenuHelper`, `AutoDungNhanh`, `AutoGiaoDich`) để tránh đè chữ/click xuyên.
3. **Hook server / an toàn:**
   - Thông báo server -> gọi từ `InfoMe.addInfo` (`OnServerInfo(s)`).
   - Mất kết nối / thoát ra màn hình đăng nhập -> gọi từ `ServerListScreen.switchToMe` (`OnDisconnectOrReset()`).
   - Nếu chặn gói tin ở `Controller` / `Controller2` để ẩn UI -> **luôn giữ nguyên phần `msg.reader().read...()`**, chỉ bọc `if (!suppressUi)` quanh phần thao tác giao diện (`Panel`, `ChatPopup`, `Menu`, `ClientInput`).

### Bước 3: Tránh 5 cạm bẫy kỹ thuật đặc thù của source NRO này
1. **Shadowing `System.Math`:** Trong `Game1` / `Game2` có sẵn class `Math`, nên bên trong `Game1.Mod` / `Game2.Mod` nếu viết `Math.Max(...)` sẽ lỗi compile `CS0117`! Luôn viết rõ **`global::System.Math.Max(...)`** (hoặc `System.Math.Max(...)` khi không xung đột).
2. **`ItemTemplates.get(short id)` có tác dụng phụ:** Hàm gốc sẽ tự nhét `"Unknown Item " + id` vào `ItemTemplates.itemTemplates` nếu ID chưa tải. Nếu chỉ kiểm tra/lấy tên hiển thị, hãy tra `ItemTemplates.itemTemplates.get((short)id) as ItemTemplate`.
3. **Dọn `ChatTextField` sau khi nhập liệu (`IChatable`):** Trong `onChatFromMe` và `onCancelChat`, luôn gọi `AutoItem.ResetTF(ChatTextField.gI());` để khôi phục `parentScreen` và đóng khung nhập.
4. **Cơ chế `Panel.timeShow` khi ẩn Panel giao dịch/shop:** `Panel.show()` đặt `timeShow = 20`, và `Panel.hide()` / `Panel.hideNow()` sẽ thoát sớm khi `timeShow > 0` (ngoài ra `Panel.hide()` với `type == 13 && !isAccept` còn tự gửi `giaodich(3)`). Nếu cần tự đóng panel vừa mở, hãy đặt cờ chờ đóng lại trong `Update()` và quản lý `isAccept` cẩn thận.
5. **Chờ đồng bộ gói cập nhật túi (`-36`):** Sau các gói đóng giao dịch (`-86, 7`) hoặc dùng/nhận đồ, có thể có độ trễ vài ms trước khi gói `-36` cập nhật `arrItemBag`; tránh kiểm tra điều kiện dừng túi ngay ở tick đóng phiên mà chưa cho phép đồng bộ túi.

### Bước 4: Kiểm thử bắt buộc trước khi build (theo `AGENTS.md`)
- Tạo/chạy kịch bản kiểm thử C# (`net8.0`, `<LangVersion>11.0</LangVersion>`) biên dịch **trực tiếp file thật** trong `DecompiledSource/Game1.Mod/<TenMod>.cs` và `DecompiledSource/Game2.Mod/<TenMod>.cs` với bộ stub nhẹ (giống mẫu `MyMod/AutoGopTests`), phủ đủ luồng chính, delay, điều kiện dừng, case biên và so khớp 100% giữa `Game1` / `Game2` / `MyMod`.
- Chỉ khi test **PASS 100%** mới chuyển sang Bước 5.

### Bước 5: Biên dịch DLL & Sao lưu
1. Chạy `powershell -ExecutionPolicy Bypass -File .\MyMod\build_mod.ps1` -> đảm bảo `0 Error(s)` và không phát sinh warning mới trong file vừa tạo.
2. Cập nhật file mới vào `$files` trong [MyMod/do_backup.ps1](file:///c:/Users/admin/Documents/Nro_GOC/MyMod/do_backup.ps1), rồi chạy `do_backup.ps1` để tạo file nén `Backup/Backup_Snapshot_<timestamp>.zip`.

### Bước 6: Ghi Log (khi người dùng yêu cầu)
- Ghi nhật ký chi tiết vào `Log/log_<tenmod>.md` và thêm mục tóm tắt mới lên đầu [Log/log_version.md](file:///c:/Users/admin/Documents/Nro_GOC/Log/log_version.md).
