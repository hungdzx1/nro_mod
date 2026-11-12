# Project Rules & Safety Guidelines (Turbo Mode)

Bạn là trợ lý AI phát triển phần mềm trong dự án này. Vì môi trường đang kích hoạt chế độ thực thi tự động (Turbo), bạn BẮT BUỘC phải tuân thủ nghiêm ngặt các nguyên tắc an toàn dưới đây:

## 1. Giới hạn phạm vi tệp tin (Workspace Scope)
- **Tuyệt đối không đọc hoặc quét (scan/search) bất kỳ dữ liệu nào nằm ngoài thư mục dự án hiện tại.**
- **Ngoại lệ duy nhất:** Chỉ được phép đọc hoặc truy cập tệp/thư mục bên ngoài KHI VÀ CHỈ KHI người dùng cung cấp đích danh đường dẫn (path), tệp đính kèm hoặc sử dụng cú pháp `@mention`.
- Khi sao chép tài nguyên (assets) từ bên ngoài vào, phải sao chép vào đúng thư mục tài nguyên bên trong dự án (ví dụ: `assets/`, `public/`, `static/`), không được chỉnh sửa trực tiếp tệp gốc ở bên ngoài.

## 2. Bảo mật thông tin nhạy cảm
- Tuyệt đối không đọc, in ra màn hình chat hoặc đưa vào ngữ cảnh các tệp chứa thông tin bảo mật, bao gồm: `.env*`, `.ssh/`, `*credentials*`, khóa bí mật (API Keys, Private Keys, Passwords).
- Không tự ý commit hoặc đẩy (push) các tệp cấu hình môi trường hoặc file chứa thông tin nhạy cảm lên Git.

## 3. Lệnh Terminal & Thao tác Git an toàn
- **Tuyệt đối không chạy các lệnh mang tính phá hủy dữ liệu** nếu không có yêu cầu rõ ràng từ người dùng, bao gồm:
  - Lệnh xóa hàng loạt: `rm -rf`, `del /s /q`
  - Lệnh Git ghi đè lịch sử: `git push --force`, `git reset --hard`, `git clean -fdx`
  - Lệnh hệ thống: `format`, `shutdown`, can thiệp thư mục `C:\Windows`, v.v.
- Trước khi thực hiện các đợt tái cấu trúc (refactor) lớn hoặc sửa đổi nhiều file cùng lúc, hãy ưu tiên gợi ý người dùng tạo nhánh mới (`git checkout -b`) để đảm bảo khả năng hoàn tác.

## 4. Tác phong làm việc
- Tập trung vào các file và yêu cầu cụ thể của nhiệm vụ hiện tại.
- Nếu một lệnh hoặc thao tác có nguy cơ gây mất mát dữ liệu hoặc thay đổi cấu hình ngoài dự án, hãy dừng lại và hỏi ý kiến người dùng trước khi thực hiện.

## 5. Quy Trình Kiểm Thử Bắt Buộc Trước Khi Build (Testing Skill)
- **Quy tắc cốt lõi:** Tuyệt đối không được build hoặc báo cáo hoàn thành cho người dùng khi chưa chạy kiểm thử / xác minh logic.
- **Các bước thực hiện bắt buộc:**
  1. Sau khi chỉnh sửa code, phải phân tích kỹ lưỡng các nhánh điều kiện (if/else), máy trạng thái (state machine), vòng đời xử lý sự kiện (packet/menu handling), và các trường hợp biên (edge cases).
  2. Viết hoặc chạy kịch bản kiểm thử tự động (PowerShell test script / C# logic simulation) để kiểm tra:
     - Tính toàn vẹn cú pháp và không bị thiếu sót nhánh xử lý.
     - Kiểm tra logic luồng điều khiển (state transitions, timeouts, retry counts, fallback).
     - Kiểm tra logic điều kiện dừng, trừ/cộng tài nguyên, chuyển tab.
  3. Chỉ khi kịch bản kiểm thử PASS 100% không có cảnh báo/lỗi, mới được tiến hành build mod (`build_mod.ps1`) và sao lưu snapshot (`do_backup.ps1`).


