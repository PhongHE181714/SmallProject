# Cấu trúc Frontend theo tính năng

## Nguyên tắc chính

Tổ chức mã nguồn ứng dụng theo từng tính năng nghiệp vụ. Đặt các lời gọi API, trạng thái Redux, kiểu dữ liệu và giao diện của một tính năng gần nhau. Đặt mã nguồn được nhiều tính năng dùng chung trong `src/shared`.

## Luồng yêu cầu và dữ liệu

```text
Page -> Feature component -> Feature API -> Shared HTTP client -> BE API
                         -> Feature Redux state -> App store
```

Các trang ghép nối những component của tính năng. Component của tính năng gửi action và đọc state. Module API của tính năng gọi HTTP client dùng chung. Frontend không kết nối trực tiếp đến SQL Server.

## Trách nhiệm của từng thư mục

| Thư mục | Trách nhiệm | Không được chứa |
|---|---|---|
| `src/app` | Redux store toàn ứng dụng, typed hooks, providers, router và cấu hình giao diện. | Xử lý SQL/API riêng của tính năng. |
| `src/pages` | Các màn hình theo route, ghép nối component của tính năng. | Lời gọi API hoặc quy tắc nghiệp vụ bị lặp lại. |
| `src/features/<feature>/api` | Các thao tác HTTP và kiểu dữ liệu phản hồi cho một tính năng. | Kết xuất React hoặc trạng thái giao diện. |
| `src/features/<feature>/model` | Redux slice, async thunk, selector và state của tính năng. | Xử lý giao diện HTTP. |
| `src/features/<feature>/components` | Component chỉ được dùng trong tính năng đó. | Component có thể tái sử dụng rộng rãi ở các tính năng không liên quan. |
| `src/shared/api` | HTTP client dùng chung và cấu hình API. | Logic riêng cho endpoint của từng tính năng. |
| `src/shared/components` | Component giao diện tái sử dụng ở nhiều tính năng. | Quy tắc nghiệp vụ riêng của tính năng. |
| `src/shared/hooks` và `src/shared/utils` | Hook dùng chung và hàm tiện ích thuần. | State tính năng API. |
| `src/i18n` | Khởi tạo i18next và tài nguyên bản dịch. | Lời gọi API hoặc logic nghiệp vụ. |
| `src/shared/styles` | Style toàn cục và token thiết kế dùng chung. | State component riêng của từng trang. |

## Thêm tính năng

1. Tạo `src/features/<feature>/api` để chứa hàm gọi endpoint và kiểu dữ liệu phản hồi.
2. Thêm `model` khi tính năng cần state Redux dùng chung. Giữ state biểu mẫu và giao diện có thời gian sống ngắn trong component.
3. Thêm giao diện chỉ dùng cho tính năng đó vào `components`.
4. Tạo trang trong `src/pages` để ghép giao diện của tính năng.
5. Thêm route vào `src/app/router.tsx` khi đã cấu hình định tuyến.
6. Đăng ký reducer của tính năng trong `src/app/store.ts` chỉ khi tính năng đó dùng Redux state.
7. Đặt nội dung hiển thị cho người dùng trong `src/i18n/locales.ts` thay vì ghi cứng trong component.

## Quy ước trách nhiệm

- Dùng Redux Toolkit cho state được chia sẻ giữa nhiều component hoặc màn hình. Không đưa mọi giá trị input vào Redux.
- Đặt đường dẫn endpoint API trong module API của tính năng. Đặt URL gốc và xử lý HTTP tổng quát trong `shared/api/httpClient.ts`.
- Dùng Material UI cho component giao diện. Chỉ đặt component bọc dùng chung trong `shared/components` khi nhiều tính năng cần chúng.
- Gọi SweetAlert2 từ tính năng cần xác nhận hoặc thông báo; không gọi thư viện này trong HTTP client dùng chung.
- Đặt tên React component và kiểu dữ liệu theo PascalCase. Đặt tên biến, hàm và hook theo camelCase; tên custom hook bắt đầu bằng `use`.
- Không tạo các thư mục kiểu backend như `Controllers`, `Services` hoặc `Repositories` trong frontend.
