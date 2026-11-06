# Kiến trúc Backend ba lớp

## Request flow

```text
Client -> Controller -> Service -> Repository -> Dapper -> SQL Server
```

Mã nguồn chỉ được gọi lớp kế tiếp theo chiều này. Controller gọi service qua interface; service xử lý nghiệp vụ và gọi repository; repository chịu trách nhiệm truy vấn/cập nhật dữ liệu. Repository không chứa quy tắc nghiệp vụ và không trả về phản hồi HTTP.

## Trách nhiệm của từng thư mục

| Thư mục | Trách nhiệm | Không được chứa |
|---|---|---|
| `Controllers` | Route HTTP, liên kết request, mã trạng thái và phản hồi API. Gọi service qua interface. | SQL, Dapper hoặc phép tính nghiệp vụ. |
| `Services/Products` | Nghiệp vụ sản phẩm. `ProductService` được chia thành các file `partial` theo nhóm query, command và file/import nhưng vẫn là một service. | SQL, Dapper hoặc phản hồi HTTP. |
| `Services/Interfaces` | Interface theo aggregate/feature, ví dụ `IProductService`. | Logic triển khai service. |
| `Services/Exceptions` | Exception nghiệp vụ mà controller ánh xạ thành mã trạng thái và ProblemDetails. | SQL hoặc phản hồi HTTP. |
| `Repositories/Products` | `ProductRepository` triển khai SQL/Dapper, chia file partial theo query, command và import. | Quy tắc nghiệp vụ và chi tiết HTTP. |
| `Repositories/Interfaces` | Interface repository theo aggregate/feature, ví dụ `IProductRepository`. | Câu lệnh SQL hoặc logic truy vấn. |
| `Models/MasterProduct` | Entity `MasterProduct` và các kiểu dữ liệu hỗ trợ riêng cho bảng/nghiệp vụ sản phẩm. | SQL và thuộc tính HTTP. |
| `Models/SaleOut` | Entity ánh xạ bảng `SaleOut`. | SQL và thuộc tính HTTP. |
| `Models/Common` | Kiểu dữ liệu dùng chung giữa các model, hiện có `PagedResult<T>`. | Entity hoặc quy tắc riêng của một tính năng. |
| `Data` | Connection factory, cấu hình hạ tầng cơ sở dữ liệu và helper SQL dùng chung như escape `LIKE`. | Quy tắc nghiệp vụ và truy vấn riêng của tính năng. |
| `DTOs` | Hợp đồng request/response API, nhóm theo tính năng. | Truy vấn Dapper và quy tắc nghiệp vụ. |
| `Migrations` | Script SQL theo thứ tự để tạo hoặc cập nhật schema và dữ liệu mẫu. | Mã ứng dụng C#. |

## Phân nhóm sản phẩm

### Service

`IProductService` là hợp đồng service duy nhất mà controller cần. `ProductService` triển khai hợp đồng này và được chia theo trách nhiệm bằng các file cùng khai báo `partial class`:

- `ProductService.Query.cs`: tìm kiếm, xác thực tiêu chí và ánh xạ dữ liệu phân trang.
- `ProductService.Commands.cs`: tạo, cập nhật, xóa và kiểm tra nghiệp vụ tương ứng.
- `ProductService.Files.cs`: import Excel/CSV, kiểm tra dữ liệu và tạo template.
- `ProductService.cs`: constructor và các repository dependency.

Các file trên là một class/service khi biên dịch; cách chia này giúp quản lý mã nguồn theo nhóm mà không tạo interface cho từng thao tác.

### Repository

`IProductRepository` là hợp đồng repository duy nhất cho sản phẩm. `ProductRepository` được chia bằng các file `partial`:

- `ProductRepository.Query.cs`: tìm kiếm và đọc sản phẩm.
- `ProductRepository.Commands.cs`: kiểm tra trùng khi tạo/cập nhật, tạo, cập nhật, kiểm tra ràng buộc và xóa.
- `ProductRepository.Import.cs`: trong transaction Serializable, nếu phát hiện mã hoặc tên đã tồn tại trong database thì rollback toàn bộ và trả lỗi theo dòng; chỉ commit khi không có bất kỳ dòng trùng nào. Dữ liệu trùng ngay trong file cũng khiến toàn bộ file bị từ chối. Endpoint giới hạn file ở 10 MiB và tối đa 10.000 dòng dữ liệu.

`ProductService` nhận `IProductRepository` qua DI. Controller không gọi repository trực tiếp.

## Tìm kiếm Sale Out

`SaleOutsController` gọi `ISaleOutService`; service xác thực tiêu chí tìm kiếm, ánh xạ model thành DTO và tính phân trang theo đơn. `SaleOutRepository` lọc đơn theo các dòng sản phẩm phù hợp, đếm và phân trang theo `SaleOutNo`, rồi trả toàn bộ sản phẩm của các đơn trên trang để phần mở rộng hiển thị đầy đủ. Tổng tiền của hàng tóm tắt là tổng `Amount` của các dòng đang hoạt động. Các truy vấn Product và Sale Out dùng chung `PagedResult<T>` và `Data/SqlLikePattern` để tránh lặp cấu trúc phân trang cùng logic escape ký tự `LIKE`.

Mã, tên và đơn vị sản phẩm trong Sale Out được lấy qua `LEFT JOIN` bằng `SaleOut.ProductId`; ngày trong bảng lưu dạng số `yyyyMMdd` và được chuyển thành ngày khi trả dữ liệu.

Tạo đơn xuất hàng nhận thông tin PO/khách hàng cùng danh sách sản phẩm. Service xác thực từng sản phẩm đang hoạt động, lấy đơn vị tính từ MasterProduct, tính số thùng và thành tiền; repository ghi cả danh sách trong transaction. Trigger cấp chung một `SaleOutNo` cho các dòng cùng PO; migration `202611060003` chuẩn hóa mã các đơn cũ và bỏ unique index trên `SaleOutNo`. Unique index `INDEX_SaleOut_CustomerPoNo_ProductId` tiếp tục chặn sản phẩm trùng trong cùng PO. Chỉnh sửa lấy toàn bộ dòng theo `SaleOutNo` và cập nhật danh sách sản phẩm trong transaction.

## Quy tắc khi thêm tính năng

1. Đặt entity và kiểu dữ liệu gắn với bảng trong `Models/<TênBảng>`; nhóm DTO theo tính năng trong `DTOs/<Feature>`.
2. Khai báo một interface repository theo aggregate/feature; chia phần triển khai thành file partial theo query/command/import khi cần.
3. Dùng một interface service theo aggregate/feature; chia file triển khai theo nhóm trách nhiệm khi cần. Chỉ tách thành nhiều service interface khi các nhóm cần ranh giới sử dụng hoặc phụ thuộc riêng.
4. Tạo controller chỉ điều phối HTTP và gọi service.
5. Đăng ký interface repository/service trong `Program.cs` với vòng đời scoped.
6. Đưa thay đổi schema hoặc dữ liệu mẫu vào migration SQL tiếp theo theo thứ tự.

## Kết nối cơ sở dữ liệu hiện tại

`Data/SqlConnectionFactory.cs` là lớp duy nhất tạo `SqlConnection`. Lớp này triển khai `Data/Interfaces/ISqlConnectionFactory.cs` và đọc `ConnectionStrings:DefaultConnection`; giá trị môi trường phát triển được đặt trong `appsettings.Development.json`.

Repository nhận `ISqlConnectionFactory`, tạo và giải phóng kết nối bằng `await using`. Import dùng một kết nối và transaction Serializable để tránh ghi dở dang.

## Quy tắc đặt tên

- Dùng PascalCase cho lớp, phương thức và thuộc tính C#; camelCase cho tham số và biến cục bộ.
- Đặt tiền tố `I` cho interface.
- Đặt tên repository method theo mục đích dữ liệu, ví dụ `SearchAsync`, `CreateAsync` và `UpdateAsync`.
