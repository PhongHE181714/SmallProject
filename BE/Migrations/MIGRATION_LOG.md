# Nhật ký migration cơ sở dữ liệu

Lưu các thay đổi cơ sở dữ liệu trong thư mục này dưới dạng các script SQL chỉ thêm mới và được đánh số theo thứ tự. Không sửa migration sau khi đã áp dụng lên cơ sở dữ liệu dùng chung; hãy tạo migration mới cho các thay đổi tiếp theo.

## Lịch sử migration

| Phiên bản | Script | Mô tả | Ngày áp dụng |
|---|---|---|---|
| 202611080001 | `202611080001_CreateFnSaleOutReport.sql` | Tạo hàm bảng `dbo.fnSaleOutReport` tổng hợp số lượng và thành tiền theo sản phẩm trong khoảng ngày. | Chưa áp dụng |
| 202611060003 | `202611060003_GroupSaleOutProductsByOrder.sql` | Gộp mã phiếu cho các sản phẩm cùng PO, bỏ unique index trên `SaleOutNo` và cập nhật trigger tạo mã theo nhóm đơn. | Chưa áp dụng |
| 202611060002 | `202611060002_BackfillSaleOutNo.sql` | Gán mã `SaleOutNo` theo tháng `OrderDate` cho các dòng SaleOut hiện có chưa có mã. | Chưa áp dụng |
| 202611060001 | `202611060001_AddSaleOutNo.sql` | Thêm `SaleOutNo`, unique filtered index và trigger cấp số phiếu `STOyyyyMMxxxx` cho các bản ghi SaleOut mới. | Chưa áp dụng |
| 202611040002 | `202611040002_RecalculateSaleOutBoxQuantity.sql` | Cập nhật số thùng các dòng SaleOut hiện có theo phép chia làm tròn lên. | Chưa áp dụng |
| 202611040001 | `202611040001_AddUniqueSaleOutCustomerPoProduct.sql` | Tạo unique filtered index cho cặp `CustomerPoNo` và `ProductId` để chặn trùng một sản phẩm trong cùng PO khách hàng. | Chưa áp dụng |
| 202611030002 | `202611030002_AddUniqueMasterProductFields.sql` | Tạo unique filtered index cho `ProductCode` và `ProductName` không `NULL`, ngăn tạo trùng sản phẩm. | Chưa áp dụng |
| 202611030001 | `202611030001_AddIsActiveToMasterProductAndSaleOut.sql` | Thêm `IsActive` vào `MasterProduct` và `SaleOut` để hỗ trợ xóa mềm; dữ liệu hiện có được đặt trạng thái đang hoạt động. | Chưa áp dụng |
| 202611020001 | `202611020001_InitialCreate.sql` | Tạo cơ sở dữ liệu `SmallProject`, hai bảng `MasterProduct` và `SaleOut`, khóa chính và khóa ngoại `SaleOut.ProductId`. | Chưa áp dụng |
| 202611020002 | `202611020002_SeedSampleData.sql` | Thêm mười sản phẩm mẫu và mười dòng `SaleOut` liên quan trong cùng một transaction. | Chưa áp dụng |

## Thêm migration

1. Tạo file SQL với số phiên bản kế tiếp, ví dụ `202611020003_AddProductIndex.sql`.
2. Ghi thay đổi schema và lý do vào bảng phía trên.
3. Rà soát và chạy script trên cơ sở dữ liệu SQL Server dự kiến.
4. Cập nhật ngày áp dụng sau khi script chạy thành công. Mỗi migration cần có một dòng riêng.

Script khởi tạo tuân theo đặc tả bảng đã cung cấp: chỉ có `Id` là bắt buộc; `SaleOut.ProductId` cho phép `NULL` và tham chiếu đến `MasterProduct.Id`; `SaleOut.OrderDate` là kiểu `int` lưu theo định dạng `yyyyMMdd`. Các dòng mẫu dùng ID cố định để script xác định chính xác những dòng được thêm; dữ liệu này dành cho môi trường phát triển/trình diễn.
