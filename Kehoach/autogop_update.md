# AUTO GỘP — ĐẶC TẢ UPDATE V2 (13/11/2026)

> Tài liệu thay thế đề xuất cho `Kehoach/autogop_update.md` trong repo `hungdzx1/nro_mod`.
> Quy ước: `### SỬA/ UPDATE ###` đánh dấu yêu cầu mới so với phiên bản hiện có.
> **Phạm vi:** giữ toàn bộ tính năng không được đề cập; cập nhật cấu hình Bánh, HN, Điểm; sửa riêng Auto đổi Tiên Phù Zeno; **KHÔNG XMAP: bỏ hoàn toàn XMap TỰ ĐỘNG đối với Zeno**.
> Bản `Backup_Snapshot_20261112_144724.zip` là mã tham khảo, **không** được ghi đè thẳng lên bản hiện tại. `PC_V2.rar` là game gốc chưa mod, chỉ dùng đối chiếu giao thức/hàm, **không** được dùng ghi đè DLL đã mod.

## I. Cấu trúc và điều khiển chung

- Tab `0: Chung`; `1: Bánh` (NPC 92); `2: HN` (NPC 56); `3: Điểm` (NPC 66); `4: Zeno` (NPC 18).
- Phím `X` mở menu mod; `F3` chạy workflow; `F4` chạy Bánh; `F5` chạy HN; `F7` chạy Điểm; `F8` chạy Zeno; `F6` hoặc `A` dừng khẩn cấp. Giữ các lệnh chat đang hoạt động (`gop`, `autogop`, `f3`, `chaygop`, `f6`, `dunggop`).
- Giữ checkbox bật từng tab, thứ tự ưu tiên, delay chuyển tab, giới hạn vòng và routing cục bộ như bản hiện tại, trừ các điều chỉnh được chỉ rõ ở dưới.
- Thứ tự mặc định của Tab Chung: Bánh (`1`) → Điểm (`3`) → Zeno (`4`) → HN (`2`). Các tab ở **hai map khác nhau**, do đó **F3 không còn có thể tự chạy trọn chu trình từ Đảo Kame sang Karin** khi đã bỏ XMap Zeno. Khi đến Zeno mà nhân vật không ở Karin, dừng/tạm dừng workflow và thông báo chính xác, tuyệt đối không tự XMap, không tự đánh dấu thiếu vé, không lặp vô hạn qua routing.
- Giữ thông báo nội bộ chống đệ quy; cho phép tùy chọn ẩn menu/UI/nhân vật/chữ bay nhưng việc ẩn UI **không được ngăn xử lý gói tin hoặc nuốt popup lỗi trước khi được ghi log**.

## II. Tab 1 — BÁNH (ID 1)

- Item ID `1337`, NPC ID `92`, map `5`, loại mua `targetBuyTypeBanh=3`.
- Hiển thị số lượng Bánh trong túi và Hồng Ngọc như hiện tại.
- Số lượng mua/đợt: giữ mặc định `batchQtyBanh=1000` và giới hạn hợp lệ hiện có.
- **### SỬA/ UPDATE ###** `delayBuyBanhMs` mặc định thành **600ms** (không còn 200ms). Cấu hình UI, dữ liệu mặc định và nhãn phải thống nhất; không ghi đè giá trị người dùng đã tự đặt khi chuyển tab.
- **### SỬA/ UPDATE ### BỎ ĐK 1 `Đủ số đợt mua`:** loại bỏ checkbox và ô `targetBatchesBanh` khỏi giao diện và luồng stop; không để tham chiếu vô hiệu trong điều kiện dừng, validation, logic routing. Nếu cần giữ field cũ để tương thích, không cho nó ảnh hưởng hành vi.
- Giữ **ĐK 2** `Đầy túi / hết tài nguyên` (`stopBanhCond2`) và **ĐK 3** `Đạt mốc Bánh` (`stopBanhCond3`, `targetItemCountBanh`, mặc định 5000), có thể cùng được bật.
- **### SỬA/ UPDATE ### DÙNG BÁNH CHỈ 1 LẦN:** khi đạt mốc hoặc khi ĐK2 kích hoạt trong khi còn Bánh và ĐK3 bật, thực hiện **chính xác một lần** gọi `UseItemInBag(1337)` cho lần kích hoạt đó; **bỏ vòng gọi lặp mỗi `delayUseBanhMs=200`**. Chờ trạng thái hành trang/server cập nhật rồi xác minh. Nếu server thực tế tiêu hết cả stack bằng một lần dùng, xác nhận sạch túi rồi hoàn tất; nếu vẫn còn Bánh, **không tự spam lần tiếp theo**, hiển thị rõ lượng còn lại và dừng/chuyển tab theo chính sách hoàn tất đã cài. Không tuyên bố 'đã dùng sạch' khi túi vẫn còn Bánh.
- Giữ định tuyến sau khi Bánh hoàn tất (`enableRoutingBanh`, đích mặc định Điểm ID 3). Tránh đệ quy StartTab trực tiếp khi nhiều điều kiện cùng kích hoạt.

## III. Tab 2 — HN (ID 2)

- **### SỬA/ UPDATE ### TÊN/Ý NGHĨA:** đổi nhãn thành **BOX HỒNG NGỌC SSS (HN)**, item `1581`. Không gọi là Hồn Nước. NPC `56`, map `5`, `targetBuyTypeHn=3`.
- Nguyên liệu mua: Zeno item ID `1378`, giá `40.000` Zeno/1 HN. Hiển thị số HN trong hành trang và số Zeno có thể sử dụng.
- `batchQtyHn` mặc định 1000; `targetBatchesHn` mặc định 50.
- **### SỬA/ UPDATE ###** `delayBuyHnMs` mặc định **600ms** (thay 200ms).
- **### SỬA/ UPDATE ### BỎ TỰ ĐỘNG DÙNG HN:** vô hiệu hóa/loại bỏ field `autoUseHn`, xóa checkbox `[v] Vừa mua vừa dùng (1581)`, không còn gọi `UseItemInBag(1581)` sau mua; vật phẩm chỉ được mua, không tự sử dụng.
- **### SỬA/ UPDATE ### ĐIỀU KIỆN DỪNG:** bỏ ĐK2 `Đầy/Hết` khỏi lựa chọn người dùng; thay bằng lựa chọn **một trong hai chế độ** (radio):
  - **ĐK1:** dừng khi đạt số đợt `targetBatchesHn`.
  - **ĐK3:** dừng **trước khi gửi yêu cầu mua** nếu `GetBagItemCount(1378) < batchQtyHn × 40.000` (phép nhân kiểu `long`, ví dụ 1000 × 40.000 = 40.000.000 Zeno).
- Dù chọn ĐK1 hoặc ĐK3, khi server báo thật sự đầy túi/thiếu nguyên liệu/hủy giao dịch vẫn phải **dừng an toàn và ghi đúng nguyên nhân**, không tiếp tục gửi gói lỗi. Đây là bảo vệ lỗi, không phải ĐK2 trên UI.
- Giữ routing HN (`enableRoutingHn`, đích mặc định Zeno ID 4), nhưng nếu Zeno khác map, áp dụng quy tắc "không tự XMap" của Tab 4, không hình thành vòng lặp.

## IV. Tab 3 — ĐIỂM (ID 3)

- NPC `66` trên map `5`; giữ phân tích điểm từ menu NPC cấp 1, bảo vệ không đọc nhầm menu cấp 2.
- Giữ 5 mốc `10Tr`, `5K`, `3K`, `200`, `50`; giữ các tùy chọn và delay `delayDiemMs=350ms`.
- **### SỬA/ UPDATE ###** `autoSelectMocDiem` mặc định **FALSE (tắt)**; khi tắt, sử dụng chính xác mốc người dùng chọn, không tự nâng/hạ mốc.
- **### SỬA/ UPDATE ###** `targetBatchesDiem` mặc định **1 lượt** (thay 50). Để mặc định 1 lượt thực sự có tác dụng, cấu hình khởi tạo `stopConditionDiem=1` (ĐK1 đủ lượt). Người dùng vẫn có thể chuyển ĐK2 nếu muốn dừng do thiếu điểm.
- Giữ routing Điểm (đích mặc định Zeno ID 4). Không tự tính là 'thành công' chỉ do gửi lệnh nếu có cách xác nhận giao dịch từ server; tối thiểu cần log trạng thái chờ xác nhận.

## V. Tab 4 — ZENO (ID 4): SỬA LỖI VÀ BỎ XMAP

### 1. Quy tắc bất biến

> Chỉ thị mới **bỏ XMap Zeno** thay thế hoàn toàn ghi chú cũ `CHỈ DÙNG CHỨC NĂNG NÀY Ở ĐẢO KAME` trong tài liệu update trước đó. Không suy diễn rằng Zeno vẫn có quyền XMap khi bắt đầu ở Đảo Kame.

- Item **Vé Zeno / Tiên Phù** ID `1583`; NPC **Thần Mèo Karin** ID `18`, map **46**.
- `batchQtyZeno=19800`, `targetBatchesZeno=1`, `delayZenoMs=350` mặc định, người dùng vẫn chỉnh được.
- Nhánh 1 **Đổi đủ đợt**: `enableRouteDoneZeno` → đích mặc định HN ID `2`.
- Nhánh 2 **Thiếu vé THẬT**: `enableRouteLackZeno` → đích mặc định Bánh ID `1`, chỉ kích hoạt sau khi xác minh `count(1583) < batchQtyZeno` hoặc server thực sự trả về lỗi thiếu vé.
- **### SỬA/ UPDATE ### XÓA HOÀN TOÀN TỰ ĐỘNG XMAP CỦA TAB ZENO.** Không gọi `StartXmapToNpc(4,46,18)`, `XmapController.StartRunToMapId(46)`, không tự `FinishXmap`, không đi/teleport/chuyển map từ map Kame tới 46 hoặc ngược lại như một phần Auto Zeno. Loại bỏ state chờ XMap `7` **khỏi đường đi của tab Zeno**. Không thay đổi tính năng XMap độc lập của các mod/tab khác nếu có.
- Nếu không ở map 46 hoặc chưa có NPC 18: thông báo rõ `Zeno yêu cầu đứng sẵn tại Tháp Karin (map 46), NPC 18` và **không** mở menu, **không** gọi nhánh thiếu vé. `F8` độc lập: kết thúc an toàn. `F3` workflow: dừng/tạm dừng toàn chu trình với lý do `WRONG_MAP_ZENO` để người chơi tự chuyển map; không tiếp tục gọi vòng qua các tab trên map 5 như thể đổi thành công.
- Chỉ tương tác NPC 18 khi xác định map 46, NPC đã tải xong, nhân vật ở vị trí hợp lệ với NPC. Đặt npcFocus chính xác; không thay tọa độ `cx/cy` và lập tức `charMove()` theo kiểu dịch chuyển giả nếu server chưa xác nhận vị trí. Nếu đã ở gần, không di chuyển thêm.

### 2. Luồng giao dịch đúng và xử lý menu

`Idle → CheckMapNpcTickets → OpenMenu(18) → WaitMenu1 → SelectExchange → WaitMenu2 → SelectZeno → WaitClientInput(-125) → SendQuantityOnce → WaitServerAck/BagUpdate → Success/CooldownOrRoute`.

- **Tham chiếu backup**, không copy nguyên: tại `Backup.../DecompiledSource/Game1.Mod/AutoGop.cs` quanh dòng 682–708, logic cũ chọn menu 1 `Đổi Tiên Phù` (thường index 3), menu 2 `Tiên Phù Zeno` (thường index 0), rồi chờ `-125`.
- Lấy **menu thực tế từ server** để xác định lựa chọn; không hardcode `3` hay `0` khi caption không khớp. Không tự chọn fallback đầu tiên khi không nhận dạng được menu. Ghi log toàn bộ caption/loại gói/index; timeout hữu hạn rồi dừng với lỗi `MENU_NOT_MATCHED`.
- Phải xử lý đúng các định dạng server và phương thức gửi tương ứng (đối chiếu `GameCanvas` và `Service` **bản gốc**):
  - Packet menu `32` → `GameScr.createMenu(...)` → `AutoGop.OnReceiveMenu` → `Service.confirmMenu(18,index)` (outgoing packet 32).
  - Packet menu `33` (`OPEN_UI_MENU`) → `OnReceiveOpenUiMenu` → `Service.menu(18,menuIndex,subIndex)` (outgoing packet 22), kể cả các mục nhiều cấp `88820/88821`; không đoán subIndex.
  - Packet `27` → `OnReceiveNewMenu` → `Service.menuId(short menuId)` (outgoing 27).
  - Packet `-125` → `Game1.Assets.src.f.Controller2.readMessage` → `OnReceiveServerInputPacket` → `Service.sendClientInput(fields)` (outgoing -125) với **đúng số trường** và kiểu dữ liệu. Nếu định dạng trường chưa biết, log và fallback có kiểm soát, không điền cùng số lượng vào những trường không phải số lượng.
- `OnReceive*` chỉ nhận và chặn menu của Zeno khi đang chạy Tab 4 và đúng trạng thái chờ tương ứng. Không đọc/tiêu thụ `msg.reader()` rồi trả `false` cho handler khác; không nuốt menu của mod khác.
- Loại bỏ logic `MaybeResetZenoMenuStepIfMainMenu` nhận dạng sai menu chính khi caption chứa `Đổi Tiên Phù Zeno`. Xác định menu cấp 1/cấp 2 qua **caption và ngữ cảnh trạng thái**. Nếu nhận lại menu chính sau khi đã chuyển state, reset có kiểm soát và mở lại đúng bước, tránh lặp menu vô tận.
- Tách `WAIT_MENU2`, `WAIT_INPUT`, `WAIT_ACK` có timeout độc lập. Khi `stepState` chuyển trạng thái phải cập nhật đồng bộ `stateTimestamp`/retryCount và chặn gói cũ. Retry tối đa số lần cấu hình, nhưng retry lại **đúng lệnh của bước đang chờ**, không gửi lại lệnh của menu khác. Hết retry báo `MENU_TIMEOUT`, `INPUT_TIMEOUT` hay `ACK_TIMEOUT` chính xác, **không** chuyển thành `FinishZenoLack`.
- Sau khi gửi số lượng, chỉ cập nhật `completedBatchesZeno`/`progressThisLoop` nếu có bằng chứng server đã xử lý thành công (ví dụ số vé ID 1583 giảm đúng lượng/packet xác nhận thành công). **Không tăng ngay khi gửi -125**. Không mặc định một lần request là một lần đổi.
- `hideUi=true` chỉ ẩn trình bày; không đóng `GameCanvas.menu`, `ClientInput`, popup trước khi các handler đã xử lý nội dung quan trọng. Thử riêng `hideUi=false` để chẩn đoán menu.
- Không phát sinh giao dịch lặp do một menu/packet bị gửi hai lần (idempotency hoặc cờ `pendingTransaction`).

### 3. Phân loại kết thúc Zeno

| Mã | Điều kiện | Hành động |
| --- | --- | --- |
| `ZENO_DONE` | Đã xác minh đủ số đợt thành công | Nhánh 1 (nếu bật) hoặc hoàn tất tab |
| `ZENO_LACK_TICKETS` | `count(1583) < batchQtyZeno`, hoặc server xác nhận thiếu vé | Nhánh 2 (nếu bật) hoặc hoàn tất tab |
| `WRONG_MAP_ZENO` | Không đứng ở map 46 / NPC18 chưa sẵn sàng | Dừng hoặc tạm dừng với hướng dẫn thủ công, **không** dùng nhánh 2 |
| `ZENO_MENU_ERROR` | Không có menu phù hợp hoặc chọn menu không phản hồi | Dừng báo lỗi, **không** dùng nhánh 2 |
| `ZENO_INPUT_TIMEOUT` | Không nhận `-125` đúng bước | Dừng báo lỗi, **không** dùng nhánh 2 |
| `ZENO_SERVER_REJECT` | Server từ chối giao dịch (không phải thiếu vé) | Dừng báo lỗi thật, không đếm thành công |

## VI. Động cơ trạng thái / Routing

- Trạng thái tổng quát các tab Bánh/HN/Điểm có thể giữ `stepState=0..6`; `7` chỉ còn dùng nếu một chức năng **khác Zeno** thực sự cần XMap. Tab Zeno không được đi qua trạng thái 7.
- Ở Zeno, dùng state/substate riêng theo menu/input/ack để không nhầm `stepState=3` (Shop của Bánh/HN) với chờ menu cấp 2 Zeno.
- Local routing ưu tiên hơn queue **chỉ khi tab thực sự hoàn tất hoặc đúng loại thiếu tài nguyên**. Lỗi map/menu/input **không phải** thành công hoặc thiếu vé.
- Tránh `StartTab` gọi đệ quy liên tiếp trong cùng frame gây loop; chuyển tab qua hàng đợi sự kiện/trạng thái có delay/giới hạn chuyển đổi.
- Ngăn vòng lặp vô hạn khi bật route `HN→Zeno→Bánh` nhưng Zeno không có mặt trên map hiện tại. Trình bày rõ trên UI rằng workflow nhiều map cần di chuyển **thủ công**.
- `progressThisLoop` chỉ tính giao dịch thành công được server xác nhận.

## VII. Bảo vệ / Tối ưu

- Giữ chống đệ quy thông báo `Notify`/`suppressInfoHook`/`inServerInfo`; giữ cache rương nếu phù hợp, nhưng **vô hiệu hóa/invalidate cache trước xác nhận inventory sau giao dịch**, không đọc cache 100ms cũ để kết luận.
- Giữ timeout hữu hạn, không đổi timeout thành hết tài nguyên một cách suy đoán. Ghi log dạng `[AutoGop][Zeno] map, npc, step, packet, caption, menuIndex, subIndex, ticketBefore, ticketAfter, elapsed, reason`.
- Gói lỗi `-26`, popup `38` phải được phân tích trước khi ẩn. `hideUi=false` phải thể hiện menu/ô nhập bình thường. Không sửa cơ chế ẩn UI của các tab khác ngoài phần cần thiết để chúng không bị ảnh hưởng.
- Bảo đảm `F6` dừng sạch và không có callback muộn của tab Zeno tiếp tục gửi giao dịch.

## VIII. Nguồn đối chiếu và phạm vi file

1. **Repo hiện tại (nguồn chính cần sửa):** `DecompiledSource/Game1.Mod/AutoGop.cs`, `DecompiledSource/Game2.Mod/AutoGop.cs`, và đúng vị trí hook nếu thật sự cần tại `Game1/Game2` `Controller.cs`, `GameScr.cs`, `Game1.Assets.src.f/Controller2.cs` và bản Game2 tương ứng.
2. **Backup chạy Zeno kiểu cũ (tham khảo, chưa hoàn thiện):** `Backup_Snapshot_20261112_144724.zip` → `DecompiledSource/Game1.Mod/AutoGop.cs` khoảng dòng 487–502, 682–708, 719–827; không được copy đè file nguyên khối.
3. **Game nguyên bản:** `PC_V2.rar` → `ProjCompleted_Data/Managed/Assembly-CSharp.dll` (không có `AutoGop`), dùng để đối chiếu `Service.openMenu`, `confirmMenu`, `menu`, `menuId`, `sendClientInput`, và menu/event của `GameCanvas`/`Controller`. Không ghi đè bản mod.
4. **Nguồn duplicate:** `MyMod/AutoGop_Game1.cs`, `MyMod/AutoGop_Game2.cs` và bản `MyMod/AutoGop/AutoGop.cs` có thể là bản sao/đời cũ; kiểm tra pipeline build trước khi sửa. `MyMod/build_mod.ps1` build từ `DecompiledSource/Assembly-CSharp.csproj`; **không sửa nhầm file không được build**.
5. Không sửa module Bánh/HN/Điểm ngoài phạm vi mục II–IV, không đụng hành vi game gốc khác.

## IX. Kiểm thử/tiêu chí chấp nhận bắt buộc

| # | Test | Kỳ vọng |
|---|---|---|
| 01 | F8 tại map 5 đủ vé | Không XMap, không mở menu NPC18; báo sai map và dừng an toàn |
| 02 | F3 tại map 5 đến lượt Zeno | Dừng/tạm dừng workflow báo cần tới Karin thủ công, không báo thiếu vé, không loop |
| 03 | F8 tại map 46, NPC18 sẵn sàng, đủ vé | Mở đúng menu cấp 1/2 → nhận -125 → gửi 1 lần đúng số lượng |
| 04 | Menu 32 có vị trí `Đổi Tiên Phù` thay đổi | Chọn theo caption thật, không hardcode index 3 |
| 05 | Menu 33 có sub-menu hoặc menu 27 có menuId | Gửi đúng phương thức `Service.menu`/`Service.menuId` và index/id thực |
| 06 | Menu không có mục đổi Zeno | Không tự chọn 0/3; báo lỗi menu rõ ràng |
| 07 | Đủ vé nhưng server không gửi -125 | Timeout đúng tên, không kích hoạt nhánh 'thiếu vé' |
| 08 | Gửi -125 nhưng giao dịch bị server từ chối | Không tăng đợt, không báo thành công |
| 09 | Đổi Zeno thành công 1 đợt, mục tiêu=1 | Xác nhận vé giảm/ack, tăng đúng 1 và chọn nhánh HN |
| 10 | Vé < batchQtyZeno từ đầu | Chỉ trường hợp này (hoặc server xác nhận thiếu vé) mới dùng nhánh Bánh |
| 11 | `hideUi=true` và `hideUi=false` | Cả hai đều đổi được; false hiện menu khi cần chẩn đoán |
| 12 | F6 khi đang chờ menu/input/ack | Dừng sạch, không gửi thêm packet/callback muộn |
| 13 | Tab Bánh đủ mốc | **Chỉ gửi 1 lệnh dùng Bánh** cho lần kích hoạt, không loop mỗi 200ms |
| 14 | Tab HN | Tên BOX HỒNG NGỌC SSS, delay 600ms, **không auto-use**; chọn dừng ĐK1 hoặc ĐK3 |
| 15 | Tab Điểm | `autoSelectMocDiem=false`, `targetBatchesDiem=1`, dừng 1 lượt mặc định |
| 16 | Game1 và Game2 | Build thành công, tương đồng hành vi, không lỗi tên namespace hoặc encoding |
| 17 | So sánh hồi quy | F4/F5/F7, UI, phím tắt, route cũ còn hoạt động trong giới hạn thay đổi yêu cầu |

### Quy trình thực hiện

- Tạo nhánh/snapshot trước sửa; so sánh diff với bản gốc và backup; chỉ chỉnh file có trong pipeline build.
- Cập nhật unit/simulation tests **thực sự bám theo logic sản phẩm**, không chỉ `Contains("...")`; thêm test các transition, packet, timeout, stop, routing, invalid menu và wrong map.
- Chạy kiểm thử 100% PASS trước build theo `AGENTS.md`; build `DecompiledSource/Assembly-CSharp.csproj` theo pipeline dự án; không tự ý chạy lệnh xóa dữ liệu hoặc thay DLL gốc tham chiếu.
- Sau build, phải thử nghiệm in-game vì static test không thể chứng minh server chấp nhận menu hay giao dịch.
- Báo cáo rõ file/dòng sửa, bug root cause đã chứng minh, test PASS/FAIL, tình trạng test game thực tế và mọi giới hạn còn lại.
