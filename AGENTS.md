# Hướng dẫn code trong workspace

Đây là các quy tắc tạm thời cho workspace do người dùng yêu cầu, dựa trên ảnh chụp màn hình `CODING RULE` đã đính kèm. Áp dụng chúng cho các tác vụ code trong workspace khi file này còn tồn tại. Đây không phải là tùy chọn chung cho mọi dự án. Nếu hướng dẫn trong tác vụ hiện tại của người dùng khác với các quy tắc này, hãy ưu tiên yêu cầu hiện tại; ở những điểm quy tắc không đề cập, giữ nguyên kiến trúc và quy ước hiện có của dự án.

Trước khi sửa code, đọc [coding-rules skill](.agents/skills/coding-rules/SKILL.md). Trước tiên, kiểm tra các file liên quan và cách triển khai hiện có. Chỉ thay đổi những gì cần cho tác vụ; không tự đặt ra yêu cầu cho tương lai. Không chạy test trừ khi người dùng yêu cầu kiểm thử hoặc xác minh.

## Lịch sử thay đổi

Với mỗi prompt làm thay đổi mã nguồn, cấu hình, migration database hoặc kiến trúc dự án, hãy thêm một mục vào [CHANGELOG.md](CHANGELOG.md) trước khi hoàn tất tác vụ. Mỗi mục phải có ngày, mục đích theo yêu cầu của người dùng, danh sách ngắn gọn các thay đổi chính và các file hoặc thư mục bị ảnh hưởng. Không thêm mục cho giải thích, đánh giá hoặc cập nhật trạng thái chỉ đọc.

