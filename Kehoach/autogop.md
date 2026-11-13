
---

# ĐẶC TẢ TÍNH NĂNG: "AUTO GỘP" (WORKFLOW AUTOMATION)

Hệ thống bao gồm **5 Tab chức năng**:
* **Tab 0: Chung** (`ID: 0`) – Bộ điều phối quy trình tổng thể.
* **Tab 1: Bánh** (`ID: 1`) – Mua & Sử dụng Bánh.
* **Tab 2: HN** (`ID: 2`) – Mua & Sử dụng HN.
* **Tab 3: Điểm** (`ID: 3`) – Đổi Điểm.
* **Tab 4: Zeno** (`ID: 4`) – Đổi Zeno.

---

### BẢNG QUẢN LÝ PHÍM TẮT TOÀN HỆ THỐNG
| Phím tắt | Tác vụ |
| :--- | :--- |
| **F3** | Chạy toàn bộ quy trình Auto Gộp (theo Tab Chung) |
| **F4** | Chạy độc lập chức năng **Bánh** |
| **F5** | Chạy độc lập chức năng **HN** |
| **F6** | **DỪNG TẤT CẢ** các tiến trình đang chạy (Global Stop) |
| **F7** | Chạy độc lập chức năng **Đim** |88
| **F7** | Chạy độc lập chức năng Zeno **Zeno** |

---

## 1. TAB CHUNG (`ID: 0`) - ĐIỀU PHỐI QUY TRÌNH
**Mục đích:** Quản lý thứ tự thực thi tuần tự của 4 tiến trình con.

* **Giao diện & Cấu hình:**
  * Danh sách 4 chức năng kèm Checkbox bật/tắt: `[v] Bánh`, `[v] HN`, `[v] Điểm`, `[v] Zeno`.
  * Ô nhập thứ tự ưu tiên (1, 2, 3, 4) cho từng chức năng đã chọn.
  * Ô nhập độ trễ (Delay) chuyển giao giữa các tiến trình (milliseconds).
  * Nút bấm: **Bắt đầu** (Phím tắt `F3`) và **Dừng** (Phím tắt `F6`).
* **Logic vận hành:**
  * Chỉ các chức năng được tích chọn mới được đưa vào hàng đợi chạy theo số thứ tự đã gán (VD: Bánh (1) → Điểm (2) → Zeno (3) → HN (4)).
  * **Quy tắc chuyển bước:** Tiến trình trước bắt buộc phải hoàn thành (kích hoạt điều kiện dừng) → Chờ hết thời gian Delay chuyển tiếp → Tự động khởi chạy tiến trình tiếp theo.

---

## 2. TAB BÁNH (`ID: 1`) - AUTO MUA & DÙNG BÁNH
Kế thừa cơ chế từ tính năng *Auto mua nhiều*, bổ sung cấu hình cố định và điều kiện dừng riêng:

* **Thông số cố định:**
  * **Item ID:** `1337`
  * **NPC ID:** `92` (Map Kame)
* **Giao diện & Thiết lập:**
  * Các ô nhập cơ bản: Số lượng mua/lần, Số lần mua, Delay giữa các lần mua.
  * Nút bấm: Chạy/Dừng riêng chức năng (Phím tắt `F4` để chạy riêng).
* **Điều kiện dừng (Chọn 1 trong 2):**
  * **Cách 1:** Dừng khi đạt đủ số lần mua **HOẶC** hết tài nguyên mua/hành trang đầy.
  * **Cách 2:** Dừng khi số lượng bánh (ID: `1337`) trong hành trang đạt mốc chỉ định.
    * *Cơ chế phụ:* Sau khi dừng, chờ một khoảng Delay cấu hình được → Tự động sử dụng hết bánh (ID: `1337`) trong rương trước khi chuyển sang chức năng kế tiếp.

---

## 3. TAB HN (`ID: 2`) - AUTO MUA & ĐỊNH TUYẾN CHUYỂN BƯỚC
Tương tự Tab Bánh nhưng áp dụng cho vật phẩm khác và có cơ chế rẽ nhánh luồng:

* **Thông số cố định:**
  * **Item ID:** `1581`
  * **NPC ID:** `56` (Map Kame, Map ID: `5`)
* **Giao diện & Thiết lập:**
  * Checkbox: **Tự động dùng vật phẩm ID 1581** có trong hành trang (sử dụng logic auto-use sẵn có).
  * Nút bấm: Chạy/Dừng riêng chức năng (Phím tắt `F5` để chạy riêng).
* **Điều kiện dừng (Chọn 1 trong 2):**
  1. Đạt đủ số lượng vật phẩm đã thiết lập.
  2. Hết tài nguyên mua hoặc hành trang bị đầy.
* **Cơ chế định tuyến cục bộ (Local Routing):**
  * Có checkbox Bật/Tắt tính năng chuyển hướng kèm điều kiện kích hoạt.
  * *Quy tắc ưu tiên:* **Luồng trong Tab HN có độ ưu tiên cao hơn quy trình tại Tab Chung**. Nếu bật, khi thỏa mãn điều kiện dừng đã chọn (VD: Điều kiện 2), hệ thống sẽ nhảy thẳng sang Tab được gán (VD: Tab Zeno - `ID: 4`) thay vì đi theo thứ tự của Tab Chung.

---

## 4. TAB ĐIỂM (`ID: 3`) - ĐỔI ĐIỂM
Giữ nguyên logic và giao diện hiện tại, bổ sung các cải tiến:

* **Nâng cấp giao diện:**
  * Hiển thị số điểm hiện có (Real-time).
  * Thêm thông báo/lưu ý trên giao diện: *"Vui lòng bật Lọc Đồ"*.
* **Điều kiện dừng (Chọn 1 trong 2):**
  * **Cách 1:** Đã đổi đủ số lượng chỉ định trước.
  * **Cách 2:** Số điểm hiện có < Số điểm tối thiểu cần để đổi.
* **Điều khiển riêng:** Phím tắt `F7` để kích hoạt riêng cho Tab này.

---

## 5. TAB ZENO (`ID: 4`) - ĐỔI VÉ ZENO
Giữ nguyên logic hiện tại, tinh chỉnh giá trị mặc định và logic rẽ nhánh song song:

* **Giá trị mặc định:**
  * Số lượng mỗi đợt: `19,800`
  * Số lượng cần đổi: `1` (người dùng có thể tùy chỉnh)
* **Nâng cấp giao diện:**
  * Hiển thị số lượng Vé Zeno (Item ID: `1583`) hiện có trong hành trang.
* **Cơ chế chuyển luồng song song (Chạy đồng thời cả 2 điều kiện):**
  * **Nhánh 1 (Đạt mục tiêu):** Khi đã đổi đủ số lượng chỉ định → Tự động chuyển sang Tab đã chọn (VD: Chuyển sang `Tab HN - ID: 2`).
  * **Nhánh 2 (Thiếu vé):** Khi số vé Zeno trong hành trang < Số lượng mỗi đợt (`19,800`) → Tự động chuyển sang Tab đã chọn (VD: Chuyển sang `Tab Bánh - ID: 1`).
* **Điều khiển riêng:** Phím tắt `F8` để kích hoạt riêng cho Tab này

*Để bật tab chức năng gộp này thì ấn X, bỏ nút Auto đổi điểm/zeno đi*

**HÃY HỎI LẠI NHỮNG THỨ CẦN THIẾT NHÉ**