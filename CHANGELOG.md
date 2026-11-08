# Lịch sử thay đổi

File này ghi lại các thay đổi được thực hiện theo prompt làm thay đổi mã nguồn, cấu hình, script database hoặc kiến trúc dự án. Thêm mục mới nhất lên đầu danh sách.

## 2026-11-08 - Hiển thị thông báo không có dữ liệu trong popup báo cáo

Mục đích: Giữ thông báo không có bản ghi bên trong popup chọn khoảng ngày báo cáo doanh thu.

Thay đổi:
- Thay hộp thoại thông báo nổi bằng thông báo thông tin ngay trong popup báo cáo.

File và thư mục bị ảnh hưởng:
- `FE/src/features/saleOut/components/SaleOutRevenueReportDialog.tsx`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-08 - Tích hợp popup xuất báo cáo doanh thu

Mục đích: Cho phép xuất báo cáo doanh thu từ màn Xuất hàng theo khoảng ngày và thông báo rõ khi khoảng ngày không có bản ghi.

Thay đổi:
- Nối nút báo cáo doanh thu với popup chọn ngày và tải file Excel từ API.
- Hiển thị thông báo khi không có dữ liệu; API trả 404 và service không tạo workbook trong trường hợp này.
- Thêm bản dịch tiếng Việt/Anh và bố cục chọn ngày thích ứng trên màn hình hẹp.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.RevenueReport.cs`
- `FE/src/features/saleOut/api/saleOutApi.ts`
- `FE/src/features/saleOut/components/SaleOutRevenueReportDialog.tsx`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-08 - Dùng hàm SQL cho báo cáo doanh thu Sale Out

Mục đích: Tách truy vấn tổng hợp báo cáo doanh thu khỏi BE để BE sử dụng hàm SQL `fnSaleOutReport(@StartDate, @EndDate)`.

Thay đổi:
- Thêm migration tạo hàm bảng `dbo.fnSaleOutReport`, tổng hợp số lượng và thành tiền theo sản phẩm trong khoảng ngày.
- Chuyển repository báo cáo doanh thu sang gọi hàm SQL cho cả truy vấn đếm và lấy dữ liệu phân trang.
- Cập nhật nhật ký migration.

File và thư mục bị ảnh hưởng:
- `BE/Migrations/202611080001_CreateFnSaleOutReport.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `BE/Repositories/SaleOuts/SaleOutRepository.Query.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Viết lại README và thêm file cấu hình mẫu

Mục đích: Giúp người nhận project chạy được ngay với hướng dẫn rõ ràng từng bước.

Thay đổi:
- Viết lại `README.md` với hướng dẫn đầy đủ: yêu cầu cài đặt, chạy migration, cấu hình BE/FE và chạy dự án.
- Thêm `FE/.env.example` — file mẫu biến môi trường FE.
- Thêm `BE/appsettings.Development.example.json` — file mẫu connection string BE.

File và thư mục bị ảnh hưởng:
- `README.md`
- `FE/.env.example`
- `BE/appsettings.Development.example.json`
- `CHANGELOG.md`

---

## 2026-11-06 - Bổ sung đầy đủ các pattern .gitignore

Mục đích: Bổ sung toàn bộ các pattern cần thiết phù hợp với stack .NET + React/Vite của dự án.

Thay đổi:
- **NuGet**: thêm `*.nupkg`, `*.snupkg`, `project.lock.json`, `artifacts/`.
- **Coverage & profiling**: thêm `*.coverage`, `coverage*/`, `lcov.info`, `*.profraw`.
- **Vite cache**: thêm `.vite/`.
- **Testing FE**: thêm thư mục output của Cypress, Playwright, Storybook.
- **OS bổ sung**: thêm `._*`, `.Spotlight-V100`, `.Trashes`, `$RECYCLE.BIN/`.
- **Temp files**: thêm `*.bak`, `*.orig`, `*.rej`.
- **Docker**: thêm `docker-compose.override.yml`.
- **Secrets/certs**: thêm `*.pem`, `*.key`, `*.pfx`, `*.p12`, `*.cer`, `*.crt`, `secrets.json`, `secrets.*.json`.

File và thư mục bị ảnh hưởng:
- `.gitignore`
- `CHANGELOG.md`

---

## 2026-11-06 - Sửa định dạng số lượng báo cáo doanh thu

Mục đích: Bỏ dấu chấm dư ở cuối số lượng trong file Excel báo cáo doanh thu.

Thay đổi:
- Định dạng cột số lượng và tổng số lượng theo số nguyên `#,##0`.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.RevenueReport.cs`
- `CHANGELOG.md`

Kiểm tra: Sẽ chạy `dotnet run` theo yêu cầu; không chạy test.

## 2026-11-06 - Sửa cách đọc ngày của API báo cáo doanh thu

Mục đích: Tránh hiểu ngày nhập theo kiểu Việt Nam `dd/MM/yyyy` thành tháng/ngày khi lọc báo cáo.

Thay đổi:
- Parse ngày báo cáo bằng định dạng tường minh `dd/MM/yyyy`, `d/M/yyyy`, ISO `yyyy-MM-dd` hoặc `yyyyMMdd` trước khi truy vấn dữ liệu.
- Dùng ngày đã parse để đặt tên file và tạo nội dung báo cáo.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa kiểm tra endpoint sau thay đổi này.

## 2026-11-06 - Sửa lỗi build và kiểm tra API báo cáo doanh thu

Mục đích: Sửa các lỗi build phát hiện khi kiểm tra chức năng xuất báo cáo doanh thu trước khi bàn giao.

Thay đổi:
- Thay thuộc tính viền EPPlus không tồn tại bằng cách đặt viền cho từng ô trong bảng Excel.
- Sửa import icon MUI, kiểu nhấn mạnh Typography và biến FE không sử dụng để build frontend thành công.
- Xác nhận API trả 400 đúng khi ngày bắt đầu lớn hơn ngày kết thúc; chưa xác nhận tải file với dữ liệu thật do môi trường hiện tại không kết nối được SQL Server vì yêu cầu mã hóa kết nối.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.RevenueReport.cs`
- `FE/src/features/apiHealth/components/ApiHealthStatus.tsx`
- `FE/src/features/saleOut/components/ImportSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/PrintSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `CHANGELOG.md`

Kiểm tra: `dotnet build BE/BE.csproj --no-restore` thành công với 4 cảnh báo nullable có sẵn; `npm run build` thành công với cảnh báo bundle lớn. API trả đúng `400` với ngày không hợp lệ; thử ngày hợp lệ bị chặn ở kết nối SQL Server.

## 2026-11-06 - Thêm API xuất báo cáo doanh thu Sale Out

Mục đích: Xuất báo cáo doanh thu sản phẩm theo khoảng ngày thành file Excel theo mẫu yêu cầu.

Thay đổi:
- Hoàn thiện truy vấn repository, lọc đơn đang hoạt động theo ngày và cộng dồn số lượng/thành tiền theo sản phẩm; hỗ trợ đếm và phân trang.
- Thêm service lấy đủ các trang dữ liệu và tạo workbook Excel gồm mã/tên sản phẩm, số lượng, thành tiền, khoảng ngày và dòng tổng.
- Thêm `GET /api/sale-outs/revenue-report?timeBegin=...&timeEnd=...`, trả file `.xlsx`; chấp nhận ngày dạng `dd/MM/yyyy`, `d/M/yyyy`, `yyyy-MM-dd` hoặc `yyyyMMdd`, đồng thời từ chối ngày bắt đầu lớn hơn ngày kết thúc.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `BE/Models/SaleOut/SaleOutRevenueReportItem.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Query.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.RevenueReport.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Căn giữa nội dung bảng phiếu PDF

Mục đích: Căn giữa các thành phần trong bảng sản phẩm của phiếu xuất hàng PDF.

Thay đổi:
- Căn giữa tiêu đề cột và nội dung các cột mã/tên sản phẩm; các cột số liệu và hàng tổng đã được căn giữa.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.Print.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Sửa lỗi Autocomplete khi mở popup in phiếu

Mục đích: Khắc phục lỗi runtime FE do cấu trúc `InputProps` của Autocomplete không tương thích với phiên bản MUI hiện tại.

Thay đổi:
- Bỏ thao tác đọc/ghi `InputProps.endAdornment` thủ công; giữ nguyên loading state và nội dung loading của Autocomplete.

File và thư mục bị ảnh hưởng:
- `FE/src/features/saleOut/components/PrintSaleOutDialog.tsx`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Tích hợp chọn và tải phiếu Sale Out trên FE

Mục đích: Cho phép người dùng chọn mã phiếu Sale Out hiện có và tải PDF trực tiếp từ màn Xuất hàng.

Thay đổi:
- Nối nút “In phiếu” với popup Autocomplete, tìm mã phiếu qua API theo `SaleOutNo` và hiển thị thông tin tóm tắt để nhận diện đơn.
- Tải PDF từ API dưới dạng file, bổ sung trạng thái đang tìm/đang tải và thông báo lỗi bằng tiếng Việt/Anh.
- Thêm kiểu dữ liệu cho danh sách chọn và hàm GET phản hồi Blob.

File và thư mục bị ảnh hưởng:
- `FE/src/features/saleOut/api/saleOutApi.ts`
- `FE/src/features/saleOut/components/PrintSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/features/saleOut/model/saleOutTypes.ts`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/api/httpClient.ts`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Sửa lỗi vòng lặp khi tạo PDF Sale Out

Mục đích: Khắc phục lỗi biên dịch do dùng thuộc tính `Count` trên mảng sản phẩm đã sắp xếp.

Thay đổi:
- Dùng `Length` để kiểm tra và duyệt mảng sản phẩm trong quá trình dựng PDF.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.Print.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Thêm API tra cứu và xuất PDF phiếu Sale Out

Mục đích: Cung cấp API cho popup chọn mã phiếu hiện có và tải phiếu xuất hàng dạng PDF theo mã đã chọn.

Thay đổi:
- Thêm API phân trang tìm mã phiếu chỉ theo SaleOutNo; PO và tên khách hàng chỉ được trả về để nhận diện đơn, không dùng làm điều kiện tìm kiếm.
- Thêm API tải PDF theo SaleOutNo, in thông tin khách hàng/đơn hàng, danh sách sản phẩm, tổng tiền và QR chứa mã phiếu.
- Trên PDF, dùng OrderDate làm ngày xuất kho, bỏ dòng ngày đặt hàng riêng và đặt phông chữ Times New Roman.
- Bật phát hiện font hệ thống cho QuestPDF để nhận diện Times New Roman được cài trên máy chạy backend.
- Đưa hàng tổng vào cuối bảng sản phẩm, gộp ba cột đầu, cộng số lượng và thành tiền, để trống cột đơn giá.
- Sắp xếp các dòng sản phẩm trong PDF tăng dần theo mã sản phẩm.
- Trả lỗi 400 khi tham số/mã phiếu không hợp lệ và 404 khi phiếu không tồn tại hoặc không còn hoạt động.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `BE/DTOs/SaleOuts/SaleOutPrintOptionsRequest.cs`
- `BE/DTOs/SaleOuts/SaleOutPrintOptionsResponse.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Query.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.Print.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Kiểm tra thông tin đơn khi import Sale Out

Mục đích: Ngăn file import thay đổi ngầm khách hàng/ngày đặt hàng và cho phép bổ sung sản phẩm mới vào PO hiện có khi thông tin đầu đơn khớp.

Thay đổi:
- Cho phép thêm các sản phẩm chưa có vào PO đã tồn tại; từ chối toàn bộ file nếu khách hàng hoặc ngày đặt hàng khác đơn hiện tại, hoặc sản phẩm đã tồn tại.
- Kiểm tra các dòng cùng PO trong file có chung khách hàng và ngày đặt hàng; lỗi trả về theo dòng Excel và không ghi một phần dữ liệu.
- Chạy kiểm tra xung đột và ghi dữ liệu trong cùng transaction; chia truy vấn theo lô phù hợp giới hạn tham số SQL Server.

File và thư mục bị ảnh hưởng:
- `BE/Models/SaleOut/SaleOutImportDuplicate.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Commands.cs`
- `BE/Services/SaleOuts/SaleOutService.Import.cs`
- `BE/Controllers/SaleOutsController.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test theo hướng dẫn của workspace.

## 2026-11-06 - Hoàn thiện quản lý đơn Sale Out nhiều sản phẩm

Mục đích: Cho phép một đơn Sale Out có nhiều sản phẩm, dùng chung mã `SaleOutNo` dạng `STOyyyyMMxxxx`, và quản lý các sản phẩm theo nhóm đơn.

Thay đổi:
- Thêm mã phiếu theo tháng và migration gán mã cho dữ liệu cũ; migration tiếp theo gom dòng cùng PO dưới một mã chung, bỏ unique index trên `SaleOutNo` và cập nhật trigger.
- Đổi API tạo để nhận nhiều sản phẩm trong một transaction; thêm API đọc/cập nhật danh sách sản phẩm theo mã phiếu. Import cũng dùng trigger để giữ mã chung theo PO.
- Tạo popup theo bố cục đơn hàng và bảng dòng sản phẩm: mã phiếu chỉ đọc, đơn vị lấy từ MasterProduct và bị khóa, số lượng/thùng lấy mặc định từ MasterProduct nhưng vẫn chỉnh sửa được; hỗ trợ thêm/xóa dòng.
- Đổi popup sửa để quản lý danh sách sản phẩm của đơn. Bảng chính hiển thị một hàng mỗi đơn, có thể thu gọn/mở rộng danh sách sản phẩm, hiện tổng tiền trên hàng đơn và giá từng sản phẩm khi mở rộng; phân trang và đếm theo đơn, sắp xếp ngày đặt hàng giảm dần, xóa cả đơn và tìm kiếm theo `SaleOut.SaleOutNo`.

File và thư mục bị ảnh hưởng:
- `BE/Migrations/202611060001_AddSaleOutNo.sql`
- `BE/Migrations/202611060002_BackfillSaleOutNo.sql`
- `BE/Migrations/202611060003_GroupSaleOutProductsByOrder.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `BE/ARCHITECTURE.md`
- `BE/Controllers/SaleOutsController.cs`
- `BE/DTOs/SaleOuts/CreateSaleOutRequest.cs`
- `BE/DTOs/SaleOuts/UpdateSaleOutOrderRequest.cs`
- `BE/DTOs/SaleOuts/SaleOutOrderListItemResponse.cs`
- `BE/Models/SaleOut/SaleOut.cs`
- `BE/Models/SaleOut/SaleOutOrder.cs`
- `BE/DTOs/SaleOuts/SaleOutCreateResponse.cs`
- `BE/DTOs/SaleOuts/SaleOutListItemResponse.cs`
- `BE/DTOs/SaleOuts/SaleOutOrderListItemResponse.cs`
- `BE/DTOs/SaleOuts/SaleOutResponse.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Commands.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Query.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `FE/src/features/saleOut/model/saleOutTypes.ts`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/features/saleOut/components/CreateSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/EditSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/SaleOutItemsEditor.tsx`
- `FE/src/features/saleOut/api/saleOutApi.ts`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build, test hoặc migration.

## 2026-11-05 - Sửa đọc ngày từ file Excel Sale Out

Mục đích: Cho phép import ngày đặt hàng theo định dạng ngày hiển thị trong file mẫu Excel.

Thay đổi:
- Đọc giá trị hiển thị của ô `OrderDate` trong `.xlsx` thay vì giá trị serial nội bộ của Excel.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.Import.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Làm rõ lỗi và gọn popup import Sale Out

Mục đích: Hiển thị lỗi import dễ đọc ngay trong popup và bỏ hướng dẫn cột khỏi màn chọn file.

Thay đổi:
- Chuyển lỗi từng dòng thành khung cảnh báo riêng, liệt kê số dòng và nội dung cần sửa phía trên bảng xem trước.
- Bỏ cột lỗi làm bảng tràn ngang; bảng lỗi chỉ dùng để đối chiếu các dòng sai.
- Bỏ phần mô tả cột bắt buộc và giới hạn file khỏi màn chọn tệp; lỗi định dạng/dung lượng vẫn báo khi phát hiện.

File và thư mục bị ảnh hưởng:
- `FE/src/features/saleOut/components/ImportSaleOutDialog.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Cập nhật cột và hiển thị lỗi import Sale Out

Mục đích: Thêm đơn giá vào file import Sale Out, lấy đơn vị tính từ MasterProduct và làm rõ các dòng có lỗi.

Thay đổi:
- Thay cột `Unit` bằng cột `Price`; giữ `QuantityPerBox` trong file và tra đơn vị/tên sản phẩm theo mã sản phẩm đang hoạt động.
- Tính thành tiền theo số lượng và đơn giá; đơn giá chỉ nhận số nguyên không âm.
- Khi import lỗi theo dòng, popup chỉ hiển thị các dòng lỗi kèm số dòng Excel và lỗi tương ứng; lỗi chung của file tiếp tục hiển thị riêng.
- Cập nhật ghi chú và mẫu Excel tiếng Việt/Anh.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.Import.cs`
- `FE/src/features/saleOut/components/ImportSaleOutDialog.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Thêm tải mẫu và import Sale Out

Mục đích: Tải file mẫu và upload nhiều phiếu xuất hàng theo yêu cầu Sale Out.

Thay đổi:
- Thêm API tạo file mẫu Excel và API import Excel/CSV, giới hạn 10 MB và 10.000 dòng.
- Kiểm tra cột bắt buộc, ngày, nội dung, số lượng nguyên dương, sản phẩm/đơn vị đang hoạt động và trùng PO + sản phẩm trong file hoặc database.
- Import toàn bộ lô trong một transaction; tra tên/đơn vị sản phẩm từ MasterProduct, tính số thùng, đơn giá mặc định 0 theo danh sách trường trong yêu cầu.
- Nối nút tải mẫu và upload trên màn Sale Out; thêm chọn file, xem trước, hiển thị lỗi theo dòng và làm mới danh sách sau import.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `BE/DTOs/SaleOuts/SaleOutImportErrorResponse.cs`
- `BE/DTOs/SaleOuts/SaleOutImportLimits.cs`
- `BE/DTOs/SaleOuts/SaleOutImportResponse.cs`
- `BE/Models/SaleOut/SaleOutImportDuplicate.cs`
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/Products/ProductRepository.Query.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Commands.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.Import.cs`
- `FE/src/features/saleOut/api/saleOutApi.ts`
- `FE/src/features/saleOut/components/ImportSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/features/saleOut/model/saleOutTypes.ts`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Sửa lặp tiền tố PO trong xác nhận xóa

Mục đích: Tránh hiển thị lặp chữ PO trong popup xác nhận khi mã đơn đã có tiền tố PO.

Thay đổi:
- Bỏ tiền tố PO cố định khỏi nội dung xác nhận xóa tiếng Việt và tiếng Anh.

File và thư mục bị ảnh hưởng:
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Thêm xóa mềm phiếu Sale Out

Mục đích: Cho phép xác nhận xóa phiếu xuất hàng và ẩn phiếu khỏi danh sách mà vẫn giữ dữ liệu lịch sử.

Thay đổi:
- Thêm API `DELETE /api/sale-outs/{id}` để chuyển phiếu đang hoạt động sang `IsActive = false`.
- Lọc danh sách Sale Out chỉ còn các phiếu đang hoạt động; không cho cập nhật phiếu đã xóa.
- Nối biểu tượng xóa với hộp thoại xác nhận có PO/sản phẩm, thông báo kết quả và tải lại danh sách sau khi xóa.
- Bổ sung nội dung xác nhận tiếng Việt và tiếng Anh.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Commands.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Query.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `FE/src/features/saleOut/api/saleOutApi.ts`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Chỉ cho nhập đơn giá nguyên trong Sale Out

Mục đích: Không cho nhập đơn giá có phần thập phân khi tạo hoặc chỉnh sửa đơn xuất hàng.

Thay đổi:
- Chặn ký tự thập phân ở hai popup và đặt bước nhập đơn giá là 1.
- Kiểm tra đơn giá là số nguyên không âm ở frontend và backend.
- Cập nhật thông báo validation tiếng Việt và tiếng Anh.

File và thư mục bị ảnh hưởng:
- `BE/Services/SaleOuts/SaleOutService.cs`
- `FE/src/features/saleOut/components/CreateSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/EditSaleOutDialog.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Khôi phục feature kiểm tra API

Mục đích: Sửa lỗi FE không phân giải được import `apiHealthSlice` và khôi phục màn kiểm tra kết nối database.

Thay đổi:
- Khôi phục Redux slice gọi `GET /api/database/ping` và lưu trạng thái kết nối.
- Khôi phục component trạng thái API được trang `ApiHealthPage` sử dụng, gồm trạng thái tải, thành công, lỗi và thao tác thử lại.

File và thư mục bị ảnh hưởng:
- `FE/src/features/apiHealth/model/apiHealthSlice.ts`
- `FE/src/features/apiHealth/components/ApiHealthStatus.tsx`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-05 - Thêm popup chỉnh sửa Sale Out

Mục đích: Cho phép chỉnh sửa đơn giá, số lượng và số lượng/thùng của đơn xuất hàng theo yêu cầu.

Thay đổi:
- Thêm API `PUT /api/sale-outs/{id}`; kiểm tra dữ liệu ở backend, chỉ cập nhật ba trường được phép và tính lại thành tiền cùng số thùng.
- Nối nút sửa từng dòng với popup; hiển thị thông tin không chỉnh sửa ở trạng thái khóa và cho sửa đơn giá, số lượng, số lượng/thùng.
- Tải lại danh sách sau khi lưu thành công; thêm thông báo tiếng Việt và tiếng Anh.
- Hỗ trợ phản hồi HTTP 204 trong hàm gọi PUT.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/SaleOutsController.cs`
- `BE/DTOs/SaleOuts/UpdateSaleOutRequest.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Commands.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `FE/src/features/saleOut/api/saleOutApi.ts`
- `FE/src/features/saleOut/components/EditSaleOutDialog.tsx`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/features/saleOut/model/saleOutTypes.ts`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/api/httpClient.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.

## 2026-11-04 - Sửa báo lỗi trùng và validation số thùng Sale Out

Mục đích: Hiển thị lỗi trùng PO/sản phẩm trong popup và đảm bảo số lượng/thùng cùng phép tính số thùng đúng nghiệp vụ.

Thay đổi:
- Hiển thị lỗi API trực tiếp trong popup để lỗi `409 Conflict` không bị lớp phủ dialog che mất.
- Tính số thùng bằng cách làm tròn lên; ví dụ `10 / 25` thành `1`; thêm migration cập nhật các dòng cũ theo công thức này.
- Bắt `Số lượng/thùng` phải là số nguyên dương ở cả FE và BE.

File bị ảnh hưởng:
- `BE/DTOs/SaleOuts/CreateSaleOutRequest.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `BE/Migrations/202611040002_RecalculateSaleOutBoxQuantity.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `BE/ARCHITECTURE.md`
- `FE/src/features/saleOut/components/CreateSaleOutDialog.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Sửa lỗi render dropdown sản phẩm trong popup Sale Out

Mục đích: Khắc phục lỗi `Cannot read properties of undefined (reading 'endAdornment')` khi mở popup tạo đơn xuất hàng.

Thay đổi:
- Chuyển phần tích hợp input của Autocomplete sang `slotProps` tương thích với phiên bản MUI hiện tại.
- Giữ nguyên ref, input props và adornment của Autocomplete khi bổ sung trạng thái tải danh sách sản phẩm.

File bị ảnh hưởng:
- `FE/src/features/saleOut/components/CreateSaleOutDialog.tsx`
- `CHANGELOG.md`

## 2026-11-04 - Tích hợp popup tạo đơn xuất hàng

Mục đích: Cho phép tạo đơn xuất hàng từ popup theo mẫu giao diện được cung cấp.

Thay đổi:
- Thêm popup hai cột dùng chung kiểu giao diện Create/Update của MasterProduct và nối nút “Thêm mới” với popup.
- Tải danh sách sản phẩm đang hoạt động từ API MasterProduct; dropdown hiển thị `Mã sản phẩm (Tên sản phẩm)`.
- Khi chọn sản phẩm, tự điền đơn vị và gợi ý số lượng/thùng; cho phép chỉnh số lượng/thùng rồi gửi dữ liệu đến `POST /api/sale-outs`.
- Thêm kiểm tra dữ liệu, hiển thị lỗi trùng từ BE và tải lại danh sách sau khi lưu thành công; bổ sung nội dung VI/EN.

File và thư mục bị ảnh hưởng:
- `FE/src/features/saleOut/`
- `FE/src/features/productMaster/model/productTypes.ts`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Chỉ nhận số lượng Sale Out là số nguyên

Mục đích: Ngăn API tạo đơn xuất hàng nhận số lượng sản phẩm dạng thập phân.

Thay đổi:
- Validation DTO yêu cầu số lượng tối thiểu là 1.
- Service từ chối số lượng có phần thập phân; đơn giá và số lượng/thùng giữ validation hiện tại.

File bị ảnh hưởng:
- `BE/DTOs/SaleOuts/CreateSaleOutRequest.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `CHANGELOG.md`

## 2026-11-04 - Dùng exception có sẵn của .NET

Mục đích: Dùng lại các loại exception có sẵn trong .NET thay cho việc tạo exception riêng cho từng nghiệp vụ/model.

Thay đổi:
- Dùng `ArgumentException` cho lỗi dữ liệu đầu vào, `InvalidOperationException` cho xung đột nghiệp vụ và `InvalidDataException` cho file import không hợp lệ.
- Cập nhật controller để tiếp tục trả các mã HTTP tương ứng; lỗi import giữ chi tiết dòng/cột trong `Exception.Data`.
- Xóa các class exception tự tạo khỏi BE.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/ProductsController.cs`
- `BE/Controllers/SaleOutsController.cs`
- `BE/Services/Products/`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `BE/Repositories/Products/ProductRepository.Commands.cs`
- `BE/Services/Exceptions/` (xóa các exception tự tạo)
- `BE/Models/MasterProduct/` (xóa các exception tự tạo)

## 2026-11-04 - Thêm API tạo đơn xuất hàng

Mục đích: Cung cấp API backend để lưu đơn xuất hàng theo popup tạo mới trong ảnh tham chiếu.

Thay đổi:
- Thêm `POST /api/sale-outs` với kiểm tra PO, khách hàng, ngày, sản phẩm, số lượng, đơn giá và số lượng/thùng.
- Service lấy sản phẩm còn hoạt động từ MasterProduct; đơn vị tính lấy từ sản phẩm, số lượng/thùng có thể nhận giá trị gợi ý hoặc giá trị người dùng chỉnh sửa.
- Backend tính số thùng theo phép chia làm tròn gần nhất và thành tiền theo số lượng nhân đơn giá.
- Repository dùng transaction để chặn trùng cặp PO khách hàng/sản phẩm; thêm migration unique index để bảo vệ tính duy nhất ở database.

File và thư mục bị ảnh hưởng:
- `BE/DTOs/SaleOuts/CreateSaleOutRequest.cs`
- `BE/DTOs/SaleOuts/SaleOutCreateResponse.cs`
- `BE/Controllers/SaleOutsController.cs`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `BE/Services/Exceptions/SaleOutCreationValidationException.cs`
- `BE/Services/Exceptions/SaleOutDuplicateException.cs`
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/Products/ProductRepository.Query.cs`
- `BE/Repositories/Interfaces/ISaleOutRepository.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Commands.cs`
- `BE/Migrations/202611040001_AddUniqueSaleOutCustomerPoProduct.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `BE/ARCHITECTURE.md`
- `CHANGELOG.md`

## 2026-11-04 - Lưu phân trang và bộ lọc trong URL

Mục đích: Giữ nguyên trang hiện tại, số dòng mỗi trang và bộ lọc đang áp dụng khi tải lại màn hình.

Thay đổi:
- Đọc và cập nhật `page`, `pageSize`, `searchField` và `searchValue` trong query string cho MasterProduct và Sale Out.
- Khởi tạo form và truy vấn API từ URL để F5 hoặc mở liên kết trực tiếp vẫn giữ nguyên kết quả.
- Ẩn query string ở trạng thái mặc định; chỉ đưa trang/số dòng khác mặc định và trường tìm kiếm có giá trị lên URL.
- Bỏ route `/api-health` đã thêm nhầm ở lượt trước; giữ route màn hình `/products` và `/sale-out`.

File bị ảnh hưởng:
- `FE/src/App.tsx`
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/shared/routing/searchablePageState.ts`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Giữ đường dẫn trang khi tải lại

Mục đích: Cho phép mở trực tiếp màn Xuất hàng và giữ nguyên màn hiện tại khi nhấn F5.

Thay đổi:
- Thêm route `/sale-out` và `/products`, cập nhật URL khi chuyển tab và đồng bộ với nút Back/Forward của trình duyệt.
- Chuẩn hóa đường dẫn gốc hoặc không xác định về `/products`.

File bị ảnh hưởng:
- `FE/src/App.tsx`
- `CHANGELOG.md`

## 2026-11-04 - Đồng bộ độ rộng cột MasterProduct theo nội dung

Mục đích: Áp dụng cách chia độ rộng theo nội dung cho bảng MasterProduct để các cột dài có thêm không gian và tiêu đề khớp dữ liệu.

Thay đổi:
- Thêm colgroup 9 cột; ưu tiên độ rộng cho tên sản phẩm và quy cách, giữ các cột số liệu/trạng thái gọn hơn.

File bị ảnh hưởng:
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

## 2026-11-04 - Ưu tiên độ rộng cho các cột chữ dài ở bảng Xuất hàng

Mục đích: Tạo thêm không gian cho Số PO khách hàng, Khách hàng và Tên sản phẩm mà vẫn giữ tiêu đề khớp với dữ liệu.

Thay đổi:
- Đặt độ rộng riêng bằng colgroup cho 13 cột; tăng các cột nội dung dài và thu gọn các cột số liệu.

File bị ảnh hưởng:
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

## 2026-11-04 - Chia đều chiều rộng các cột bảng Xuất hàng

Mục đích: Căn các tiêu đề và ô dữ liệu Sale Out thành những cột có chiều rộng bằng nhau.

Thay đổi:
- Cố định bố cục bảng, chia đều chiều rộng 13 cột và cho nội dung dài xuống dòng bên trong ô.

File bị ảnh hưởng:
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

## 2026-11-04 - Bổ sung các nút thao tác cho màn Xuất hàng

Mục đích: Hoàn thiện bố cục màn Xuất hàng theo ảnh tham chiếu bằng cách thêm các nút thao tác còn thiếu.

Thay đổi:
- Thêm nút thêm mới, tải file mẫu, tải lên file, in phiếu và báo cáo doanh thu.
- Thêm nút sửa/xóa ở từng dòng; các nút hiện chỉ hiển thị, chưa gắn xử lý.
- Thêm nhãn tiếng Việt và tiếng Anh cho các nút.

File bị ảnh hưởng:
- `FE/src/features/saleOut/components/SaleOutView.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Tạo màn tra cứu Xuất hàng trên FE

Mục đích: Xây dựng màn hình Sale Out theo thiết kế được cung cấp, dùng API thực tế với tìm kiếm theo trường và phân trang.

Thay đổi:
- Thêm trang Sale Out, điều hướng giữa Master sản phẩm và Xuất hàng, hỗ trợ tiếng Việt/Anh.
- Kết nối `GET /api/sale-outs`, chọn một trong năm trường API hỗ trợ, phân trang và đổi số dòng mỗi trang.
- Hiển thị các cột đơn hàng, khách hàng, sản phẩm, số lượng và tiền; bổ sung đơn vị tính vào dữ liệu API từ `MasterProduct`.

File và thư mục bị ảnh hưởng:
- `FE/src/App.tsx`
- `FE/src/pages/SaleOutPage.tsx`
- `FE/src/features/saleOut`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/styles/global.css`
- `BE/Models/SaleOut/SaleOut.cs`
- `BE/DTOs/SaleOuts/SaleOutListItemResponse.cs`
- `BE/Repositories/SaleOuts/SaleOutRepository.Query.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `BE/ARCHITECTURE.md`
- `CHANGELOG.md`

## 2026-11-04 - Hoàn thiện tìm kiếm Sale Out và dùng chung phần phân trang

Mục đích: Giảm lặp giữa truy vấn tìm kiếm Sale Out và Product, đồng thời hoàn thiện luồng đọc Sale Out theo kiến trúc hiện tại.

Thay đổi:
- Thay hai kiểu kết quả phân trang riêng bằng `PagedResult<T>` và dùng chung helper escape ký tự `LIKE`.
- Hoàn thiện service, controller và đăng ký DI cho tìm kiếm Sale Out.
- Join `MasterProduct` qua `ProductId` để tìm và trả mã/tên sản phẩm; chuyển ngày `yyyyMMdd` từ database thành ngày trong response.

File và thư mục bị ảnh hưởng:
- `BE/Models/Common/PagedResult.cs`
- `BE/Data/SqlLikePattern.cs`
- `BE/Models/MasterProduct/ProductSearchCriteria.cs`
- `BE/Models/SaleOut`
- `BE/Repositories/Interfaces`
- `BE/Repositories/Products/ProductRepository.Query.cs`
- `BE/Repositories/SaleOuts`
- `BE/DTOs/SaleOuts`
- `BE/Services/Interfaces/ISaleOutService.cs`
- `BE/Services/SaleOuts/SaleOutService.cs`
- `BE/Services/Exceptions/SaleOutSearchValidationException.cs`
- `BE/Controllers/SaleOutsController.cs`
- `BE/Program.cs`
- `BE/ARCHITECTURE.md`
- `CHANGELOG.md`

## 2026-11-04 - Chỉ hiển thị mã sản phẩm bị trùng khi import

Mục đích: Thông báo lỗi import chỉ nêu mã sản phẩm đã tồn tại và dòng tương ứng, không liệt kê tên sản phẩm trùng.

Thay đổi:
- FE giữ thông báo chặn toàn bộ file và danh sách mã sản phẩm kèm số dòng; ẩn chi tiết lỗi trùng tên.

File bị ảnh hưởng:
- `FE/src/features/productMaster/components/ImportProductDialog.tsx`
- `CHANGELOG.md`

## 2026-11-04 - Chặn toàn bộ file import khi có dữ liệu trùng

Mục đích: Đảm bảo file import chỉ được ghi khi toàn bộ dữ liệu hợp lệ và không trùng sản phẩm đã có.

Thay đổi:
- Repository rollback transaction và trả danh sách mã/tên trùng nếu phát hiện bất kỳ dòng nào đã tồn tại; không thêm các dòng còn lại.
- Service ánh xạ dữ liệu trùng về số dòng và trường lỗi; duplicate trong file tiếp tục chặn toàn bộ file.
- FE hiển thị các mã/dòng trùng, thông báo không có dòng nào được thêm; bỏ `SkippedCount` khỏi response import.

File và thư mục bị ảnh hưởng:
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/Products/ProductRepository.Import.cs`
- `BE/Services/Products/ProductService.Files.cs`
- `BE/DTOs/Products/ProductImportResponse.cs`
- `BE/ARCHITECTURE.md`
- `FE/src/features/productMaster/components/ImportProductDialog.tsx`
- `FE/src/features/productMaster/model/productTypes.ts`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Cho phép tải file trực tiếp từ popup import

Mục đích: Cho phép người dùng chọn xem trước hoặc tải file lên ngay mà không bắt buộc đi qua màn preview.

Thay đổi:
- Nút ở bước chọn file gọi API import trực tiếp; nhấn vào tên file vẫn mở màn preview và nút xác nhận trong preview tiếp tục hoạt động.
- Thêm nhãn tải lên riêng cho tiếng Việt và tiếng Anh.

File bị ảnh hưởng:
- `FE/src/features/productMaster/components/ImportProductDialog.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Hoàn thiện popup import sản phẩm trên FE

Mục đích: Kết nối popup tải file sản phẩm với API import thật, hỗ trợ xem trước nội dung và hiển thị kết quả/lỗi dễ hiểu.

Thay đổi:
- Gửi file `.xlsx`/`.csv` bằng `multipart/form-data` tới API import.
- Thêm preview phía trình duyệt: nhấn tên file để xem bảng dữ liệu trước khi xác nhận; giới hạn preview 20 dòng đầu.
- Chặn file lớn hơn 10 MiB và file vượt 10.000 dòng trước khi tải lên; hiển thị lỗi BE, đổi lỗi trùng mã thành thông báo `Đã tồn tại các mã sản phẩm`.
- Khi thành công, chỉ thông báo số dòng mới được thêm; làm mới danh sách sản phẩm.
- Đồng bộ popup và bản dịch Việt/Anh với popup create/update.

File và thư mục bị ảnh hưởng:
- `FE/package.json`
- `FE/package-lock.json`
- `FE/src/shared/api/httpClient.ts`
- `FE/src/features/productMaster/api/productsApi.ts`
- `FE/src/features/productMaster/model/productTypes.ts`
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/features/productMaster/components/ImportProductDialog.tsx`
- `FE/src/shared/styles/global.css`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

## 2026-11-04 - Giới hạn dung lượng và số dòng file import

Mục đích: Giới hạn tài nguyên sử dụng khi nhận và xử lý file Excel/CSV import sản phẩm.

Thay đổi:
- Giới hạn dung lượng file ở 10 MiB và trả HTTP 413 khi file vượt mức cho phép.
- Giới hạn tối đa 10.000 dòng dữ liệu; kiểm tra trước khi tạo danh sách dòng cho Excel và trong khi đọc CSV.
- Giới hạn kích thước request/multipart riêng trên endpoint import, có phần đệm cho multipart headers.

File và thư mục bị ảnh hưởng:
- `BE/DTOs/Products/ProductImportLimits.cs`
- `BE/Controllers/ProductsController.cs`
- `BE/Services/Products/ProductService.Files.cs`
- `BE/ARCHITECTURE.md`
- `CHANGELOG.md`

## 2026-11-04 - Sửa Swagger cho API tải file sản phẩm

Mục đích: Khắc phục lỗi SwaggerGen khi tạo tài liệu cho endpoint import sản phẩm nhận `IFormFile`.

Thay đổi:
- Bỏ `[FromForm]` gắn trực tiếp trên tham số `IFormFile`; ASP.NET Core tự suy ra binding nguồn file, endpoint vẫn nhận `multipart/form-data`.

File bị ảnh hưởng:
- `BE/Controllers/ProductsController.cs`
- `CHANGELOG.md`

## 2026-11-04 - Cấu trúc backend ba lớp và phân nhóm theo partial class

Mục đích: Ghi lại cấu trúc backend hiện tại sau khi tổ chức lại service, repository và model theo bảng.

Kết quả hiện tại:
- Request đi theo luồng `Controller → IProductService/ProductService → IProductRepository/ProductRepository → Dapper → SQL Server`; controller chỉ xử lý HTTP, service xử lý nghiệp vụ và repository truy vấn dữ liệu.
- `ProductService` có một interface `IProductService`, chia triển khai bằng partial files: query, commands và files/import/template.
- `ProductRepository` có một interface `IProductRepository`, chia triển khai bằng partial files: query, commands và import.
- Model được nhóm theo bảng trong `Models/MasterProduct` và `Models/SaleOut`; validation/import exceptions nằm trong `Services/Exceptions`, còn exception gắn với quy tắc dữ liệu sản phẩm nằm trong `Models/MasterProduct`.
- DI đăng ký một service và một repository sản phẩm theo vòng đời scoped.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/ProductsController.cs`
- `BE/Program.cs`
- `BE/Services/Interfaces/IProductService.cs`
- `BE/Services/Products/`
- `BE/Services/Exceptions/`
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/Products/`
- `BE/Models/MasterProduct/`
- `BE/Models/SaleOut/`
- `BE/ARCHITECTURE.md`

## 2026-11-04 - Thêm API tải lên danh sách sản phẩm từ Excel và CSV

Mục đích: Cung cấp API để tải lên file `.xlsx` hoặc `.csv`, kiểm tra dữ liệu sản phẩm và chỉ thêm toàn bộ danh sách khi file hợp lệ.

Thay đổi:
- Thêm endpoint `POST /api/products/import` nhận file multipart, trả lỗi theo dòng/trường khi thiếu hoặc sai dữ liệu và báo trùng mã/tên sản phẩm.
- Đọc template Excel/CSV, kiểm tra các trường bắt buộc, giới hạn dữ liệu và trùng lặp trong file hoặc trên hệ thống.
- Thêm thao tác import trong repository với transaction Serializable để đảm bảo file được thêm toàn bộ hoặc không thêm dòng nào.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/ProductsController.cs`
- `BE/DTOs/Products/ProductImportResponse.cs`
- `BE/DTOs/Products/ProductImportErrorResponse.cs`
- `BE/Models/ProductImportDuplicate.cs`
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/ProductRepository.cs`
- `BE/Services/Interfaces/IProductService.cs`
- `BE/Services/ProductService.cs`
- `BE/Services/ProductImportException.cs`
- `CHANGELOG.md`


## 2026-11-03 - Nâng cấp tải file mẫu sản phẩm sang định dạng Excel chuẩn bằng EPPlus

Mục đích: Chuyển đổi tính năng tải file mẫu nhập liệu sản phẩm từ CSV tĩnh ở frontend sang API backend trả về file Excel (`.xlsx`) chuẩn dùng thư viện EPPlus; định dạng header, tự động căn chỉnh độ rộng cột (AutoFitColumns) không bị mất chữ và bổ sung dữ liệu mẫu hướng dẫn nhập liệu.

Thay đổi:
- BE: Cài đặt gói thư viện NuGet `EPPlus` phiên bản 7.6.0.
- BE: Thêm endpoint `GET /api/products/template` trong [`ProductsController`](file:///e:/Work/Week1/Smallproject/BE/Controllers/ProductsController.cs) tạo file Excel `.xlsx` với tiêu đề tô nền xanh đậm, chữ trắng in đậm, kèm 3 dòng dữ liệu mẫu và tính năng tự động vừa khít cột (`AutoFitColumns`).
- FE: Thêm hàm `downloadProductTemplate` trong [`productsApi.ts`](file:///e:/Work/Week1/Smallproject/FE/src/features/productMaster/api/productsApi.ts) gọi API tải blob file `.xlsx`.
- FE: Cập nhật hàm `downloadTemplate` trong [`ProductMasterView.tsx`](file:///e:/Work/Week1/Smallproject/FE/src/features/productMaster/components/ProductMasterView.tsx) gọi API và tải file `master-product-template.xlsx`.
- FE: Bổ sung thông điệp đa ngôn ngữ cho lỗi tải file mẫu trong [`locales.ts`](file:///e:/Work/Week1/Smallproject/FE/src/i18n/locales.ts).

File và thư mục bị ảnh hưởng:
- `BE/BE.csproj`
- `BE/Controllers/ProductsController.cs`
- `FE/src/features/productMaster/api/productsApi.ts`
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Đã kiểm tra build BE (`dotnet build`) và FE (`npm run build`), xác thực tải file qua endpoint trả về file Excel thành công. Chưa chạy kiểm thử tự động (theo quy tắc coding).


## 2026-11-03 - Thêm API và luồng giao diện xóa sản phẩm có kiểm tra ràng buộc

Mục đích: Triển khai chức năng xóa sản phẩm theo quy trình xác nhận; kiểm tra ràng buộc giao dịch (SaleOut) trước khi xóa để chặn các sản phẩm đã phát sinh dữ liệu, cho phép xóa trực tiếp nếu chưa có ràng buộc.

Thay đổi:
- BE: Thêm model exception `ProductDeletionBlockedException`.
- BE: Bổ sung các phương thức `ExistsAsync`, `HasRelatedTransactionsAsync`, `DeleteAsync` trong `IProductRepository` và `ProductRepository`.
- BE: Bổ sung nghiệp vụ `DeleteAsync` trong `IProductService` và `ProductService` kiểm tra tồn tại và kiểm tra ràng buộc `SaleOut` trước khi xóa.
- BE: Thêm endpoint `DELETE /api/products/{id:guid}` trong `ProductsController`, trả về HTTP 204 khi thành công, HTTP 409 khi bị chặn xóa do ràng buộc.
- FE: Bổ sung hàm `del` trong `httpClient.ts` và hàm `deleteProduct` trong `productsApi.ts`.
- FE: Tích hợp popup xác nhận bằng SweetAlert2 theo đúng yêu cầu: "Có chắc chắn muốn xóa sản phẩm hay không?" với hai lựa chọn Có / Không; hiển thị thông báo chặn xóa nếu sản phẩm đã phát sinh giao dịch.
- FE: Cập nhật i18n cho `deleteDialog` trong `locales.ts` (tiếng Việt và tiếng Anh).

File và thư mục bị ảnh hưởng:
- `BE/Models/ProductDeletionBlockedException.cs`
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/ProductRepository.cs`
- `BE/Services/Interfaces/IProductService.cs`
- `BE/Services/ProductService.cs`
- `BE/Controllers/ProductsController.cs`
- `FE/src/shared/api/httpClient.ts`
- `FE/src/features/productMaster/api/productsApi.ts`
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Đã kiểm tra build FE (`npm run build`). Chưa chạy kiểm thử tự động (theo quy tắc coding).


## 2026-11-03 - Khóa trường mã sản phẩm trong popup chỉnh sửa sản phẩm

Mục đích: Không cho phép chỉnh sửa trường Mã sản phẩm khi cập nhật thông tin sản phẩm bằng cách vô hiệu hóa (disabled) ô nhập liệu này.

Thay đổi:
- Đặt thuộc tính `disabled` cho ô nhập Mã sản phẩm (`update-product-code`) trong `UpdateProductDialog.tsx`.
- Chuyển `autoFocus` sang trường Tên sản phẩm để người dùng bắt đầu chỉnh sửa nhanh chóng.
- Thêm định dạng CSS hiển thị trực quan cho ô nhập bị vô hiệu hóa trong popup (`global.css`).

File và thư mục bị ảnh hưởng:
- `FE/src/features/productMaster/components/UpdateProductDialog.tsx`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: Đã kiểm tra build FE (`npm run build`). Chưa chạy kiểm thử tự động (theo quy tắc coding).


## 2026-11-03 - Thêm popup cập nhật sản phẩm kèm trường IsActive và tích hợp API

Mục đích: Xây dựng popup cập nhật sản phẩm tương tự form tạo mới, bổ sung trường chọn trạng thái IsActive (Đang hoạt động / Dừng hoạt động) và kết nối với API cập nhật `PUT /api/products/{id}`.

Thay đổi:
- Bổ sung hàm `put` vào `httpClient.ts` và hàm `updateProduct` vào `productsApi.ts`.
- Bổ sung `Id` vào `ProductListItemResponse` (BE) và `ProductListItem` (FE) để xác định bản ghi khi cập nhật.
- Xây dựng component `UpdateProductDialog.tsx` kế thừa cấu trúc form 2 cột từ `CreateProductDialog`, bổ sung trường chọn trạng thái `IsActive` (Active/Inactive) dạng Select dropdown.
- Tự động điền dữ liệu của sản phẩm được chọn vào form khi mở popup sửa.
- Kết nối nút Sửa (icon Edit) trên từng hàng của bảng danh mục trong `ProductMasterView.tsx` để mở popup cập nhật, tự động làm mới danh sách sau khi lưu.
- Bổ sung bản dịch i18n cho `updateDialog` trong `locales.ts` (tiếng Việt và tiếng Anh).

File và thư mục bị ảnh hưởng:
- `BE/DTOs/Products/ProductListItemResponse.cs`
- `BE/Services/ProductService.cs`
- `BE/Repositories/ProductRepository.cs`
- `FE/src/shared/api/httpClient.ts`
- `FE/src/features/productMaster/model/productTypes.ts`
- `FE/src/features/productMaster/api/productsApi.ts`
- `FE/src/features/productMaster/components/UpdateProductDialog.tsx`
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/shared/styles/global.css`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Đã kiểm tra build FE (`npm run build`). Chưa chạy kiểm thử tự động (theo quy tắc coding).


## 2026-11-03 - Cập nhật điều kiện validate số lượng và trọng lượng lớn hơn 0

Mục đích: Siết điều kiện kiểm tra dữ liệu cho số lượng/thùng và trọng lượng thành giá trị lớn hơn 0 (> 0), giữ nguyên các thông báo lỗi hiện có.

Thay đổi:
- Đổi điều kiện kiểm tra `QuantityPerBox` và `ProductWeight` từ `>= 0` thành `> 0` (chặn các giá trị `<= 0`).
- Áp dụng đồng bộ ở cả service backend (`ProductService.cs`) và form frontend (`CreateProductDialog.tsx`).
- Giữ nguyên các chuỗi thông báo lỗi hiện tại theo yêu cầu.

File và thư mục bị ảnh hưởng:
- `BE/Services/ProductService.cs`
- `FE/src/features/productMaster/components/CreateProductDialog.tsx`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test (theo quy tắc coding).


## 2026-11-03 - Thêm popup tạo sản phẩm và tích hợp API backend vào giao diện

Mục đích: Xây dựng popup tạo mới sản phẩm theo wireframe thiết kế, đồng bộ giao diện trang Master sản phẩm hiện tại, kiểm tra tính hợp lệ của dữ liệu bắt buộc và tích hợp gọi API backend để lưu sản phẩm vào hệ thống.

Thay đổi:
- Xây dựng component `CreateProductDialog` dạng 2 cột theo format wireframe: Mã sản phẩm, Tên sản phẩm, Đơn vị, Quy cách, Số lượng/Thùng, Trọng lượng cùng 2 nút Đóng và Lưu.
- Áp dụng phong cách giao diện trang nhã, nhất quán với hệ thống màu và typography của Master sản phẩm; hỗ trợ responsive tự động chuyển 1 cột trên màn hình nhỏ.
- Kiểm tra tính hợp lệ dữ liệu phía FE: bắt buộc nhập tất cả các trường, giới hạn độ dài chuỗi, số lượng/thùng là số nguyên không âm, trọng lượng là số không âm (hỗ trợ cả dấu `.` và `,`, tối đa 3 chữ số thập phân).
- Tích hợp hàm `post` trong `httpClient` và hàm `createProduct` trong `productsApi`, tự động làm mới danh sách sản phẩm sau khi lưu thành công và hiển thị thông báo.
- Xử lý lỗi trùng mã sản phẩm theo đúng quy định: "Mã sản phẩm {mã} đã tồn tại, vui lòng nhập lại".
- Bổ sung kiểm tra trường bắt buộc ở service backend và làm rõ thông báo lỗi trùng mã sản phẩm ở controller backend.
- Cập nhật i18n cho cả tiếng Việt và tiếng Anh.

File và thư mục bị ảnh hưởng:
- `FE/src/shared/api/httpClient.ts`
- `FE/src/features/productMaster/model/productTypes.ts`
- `FE/src/features/productMaster/api/productsApi.ts`
- `FE/src/features/productMaster/components/CreateProductDialog.tsx`
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/shared/styles/global.css`
- `FE/src/i18n/locales.ts`
- `BE/Controllers/ProductsController.cs`
- `BE/Services/ProductService.cs`
- `CHANGELOG.md`

Kiểm tra: Đã kiểm tra build FE (`npm run build`), kiểm tra proxy endpoint và xử lý lỗi duplicate trên backend. Chưa chạy kiểm thử tự động (theo quy tắc coding).


## 2026-11-03 - Yêu cầu số lượng mỗi thùng là số nguyên không âm

Mục đích: Không cho nhập số lượng/thùng dạng phân số hoặc số âm, đồng thời giữ nguyên kiểu `DECIMAL` trong database.

Thay đổi:
- Kiểm tra khi tạo sản phẩm và tìm kiếm rằng `QuantityPerBox` là số nguyên từ 0 đến giới hạn `INT`.
- Cập nhật thông báo lỗi cho API và FE; trọng lượng vẫn hỗ trợ số thập phân.
- Không thay đổi schema hoặc migration database.

File và thư mục bị ảnh hưởng:
- `BE/Services/ProductService.cs`
- `BE/Program.cs`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test.


## 2026-11-03 - Kiểm tra trùng sản phẩm và làm rõ lỗi nhập số

Mục đích: Ngăn tạo sản phẩm trùng mã/tên và trả thông báo dễ hiểu khi dữ liệu số không hợp lệ.

Thay đổi:
- Kiểm tra trùng `ProductCode` và `ProductName`, trả HTTP 409 với thông báo riêng cho từng trường.
- Thêm unique filtered index cho hai cột, đồng thời kiểm tra trùng hiện có trước khi áp dụng migration.
- Thay lỗi parse JSON chung bằng thông báo riêng cho số lượng/thùng và trọng lượng; giữ hỗ trợ giá trị thập phân đến 3 chữ số.

File và thư mục bị ảnh hưởng:
- `BE/Program.cs`
- `BE/Controllers/ProductsController.cs`
- `BE/Repositories`
- `BE/Services/ProductService.cs`
- `BE/Models/ProductDuplicateException.cs`
- `BE/Migrations/202611030002_AddUniqueMasterProductFields.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test; cần áp dụng migration `202611030002` sau migration `202611030001`.


## 2026-11-03 - Thêm API tạo sản phẩm

Mục đích: Cho phép tạo sản phẩm mới trong `MasterProduct` qua backend theo mô hình ba lớp.

Thay đổi:
- Thêm endpoint `POST /api/products`, DTO tạo/response và các phương thức tương ứng ở service, repository.
- BE tự sinh `Id`, đặt `IsActive = true`, chuẩn hóa chuỗi trống thành `NULL` và kiểm tra độ dài/độ chính xác `DECIMAL(18,3)`.
- Dùng Dapper với tham số SQL để thêm bản ghi.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/ProductsController.cs`
- `BE/DTOs/Products`
- `BE/Repositories/Interfaces/IProductRepository.cs`
- `BE/Repositories/ProductRepository.cs`
- `BE/Services/Interfaces/IProductService.cs`
- `BE/Services/ProductService.cs`
- `BE/Services/ProductCreationValidationException.cs`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test; cần áp dụng migration `202611030001` để database có cột `IsActive`.


## 2026-11-03 - Hiển thị trạng thái Master Product

Mục đích: Trả và hiển thị trạng thái hoạt động của sản phẩm theo giá trị `IsActive`.

Thay đổi:
- Bổ sung `IsActive` vào model, truy vấn Dapper và response API sản phẩm.
- Thêm cột trạng thái trên FE, hiển thị “Đang hoạt động” khi giá trị là `1`, “Dừng hoạt động” khi là `0`.
- Bổ sung nhãn tiếng Anh và định dạng badge cho trạng thái.

File và thư mục bị ảnh hưởng:
- `BE/Models/MasterProduct.cs`
- `BE/Repositories/ProductRepository.cs`
- `BE/DTOs/Products/ProductListItemResponse.cs`
- `BE/Services/ProductService.cs`
- `FE/src/features/productMaster`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test; cần áp dụng migration `202611030001` trước khi gọi API.


## 2026-11-03 - Bổ sung trạng thái xóa mềm cho đơn hàng

Mục đích: Áp dụng cùng cơ chế giữ dữ liệu khi xóa mềm cho cả sản phẩm và đơn hàng.

Thay đổi:
- Mở rộng migration chưa áp dụng để thêm `IsActive BIT NOT NULL DEFAULT (1)` cho `SaleOut` bên cạnh `MasterProduct`.
- Đảm bảo các dòng sản phẩm và đơn hàng hiện có được đặt trạng thái đang hoạt động.
- Cập nhật tên file migration và nhật ký migration.

File và thư mục bị ảnh hưởng:
- `BE/Migrations/202611030001_AddIsActiveToMasterProductAndSaleOut.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy migration trên SQL Server.


## 2026-11-03 - Thêm migration trạng thái hoạt động sản phẩm

Mục đích: Chuẩn bị cơ sở dữ liệu cho chức năng xóa mềm sản phẩm, giữ nguyên sản phẩm được tham chiếu từ lịch sử bán hàng.

Thay đổi:
- Thêm cột `MasterProduct.IsActive` kiểu `BIT`, `NOT NULL`, mặc định `1`; dữ liệu sản phẩm đang có được giữ ở trạng thái hoạt động.
- Ghi phiên bản migration vào `dbo.__SchemaMigrations` trong cùng transaction.
- Cập nhật nhật ký migration; không sửa các migration đã có.

File và thư mục bị ảnh hưởng:
- `BE/Migrations/202611030001_AddIsActiveToMasterProduct.sql`
- `BE/Migrations/MIGRATION_LOG.md`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy migration trên SQL Server.


## 2026-11-03 - Thêm nút đổi ngôn ngữ cho Master Product

Mục đích: Cho phép người dùng chuyển giao diện trang Master Product giữa tiếng Việt và tiếng Anh.

Thay đổi:
- Thêm nút VI/EN ở phần đầu trang; ngôn ngữ đang chọn được làm nổi bật.
- Kết nối nút với i18n hiện có và bổ sung nhãn hỗ trợ tiếp cận cho bộ chọn ngôn ngữ.

File và thư mục bị ảnh hưởng:
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/shared/styles/global.css`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test.


## 2026-11-02 - Căn giữa cột số lượng và trọng lượng

Mục đích: Căn chỉnh thống nhất tiêu đề và dữ liệu của hai cột số lượng/thùng, trọng lượng.

Thay đổi:
- Căn giữa tiêu đề cột và giá trị sản phẩm trong bảng.

File và thư mục bị ảnh hưởng:
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy build hoặc test.


## 2026-11-02 - Tách hướng dẫn lỗi số theo trường sản phẩm

Mục đích: Hiển thị ví dụ nhập liệu phù hợp riêng cho số lượng/thùng và trọng lượng.

Thay đổi:
- Tạo thông báo riêng cho `QuantityPerBox` và `ProductWeight` bằng tiếng Việt và tiếng Anh.

File và thư mục bị ảnh hưởng:
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/i18n/locales.ts`
- `CHANGELOG.md`

Kiểm tra: `npm run build` thành công; Vite báo bundle JS lớn hơn 500 kB sau minify.


## 2026-11-02 - Căn giữa và làm rõ thông báo lỗi tải sản phẩm

Mục đích: Hiển thị lỗi dễ hiểu, căn giữa trong vùng bảng thay vì đưa mã HTTP thô cho người dùng.

Thay đổi:
- Chuyển lỗi tải vào giữa phần thân bảng, thêm tiêu đề và nút thử lại.
- Ánh xạ lỗi nhập liệu, mất kết nối, API không tồn tại và lỗi máy chủ thành thông báo thân thiện.
- Xóa dữ liệu trang cũ khi request thất bại để không hiển thị tổng số và phân trang bị sai.

File và thư mục bị ảnh hưởng:
- `FE/src/features/productMaster/components/ProductMasterView.tsx`
- `FE/src/shared/api/httpClient.ts`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: `npm run build` thành công; Vite báo bundle JS lớn hơn 500 kB sau minify.


## 2026-11-02 - Xây dựng giao diện Master sản phẩm

Mục đích: Tạo màn hình danh sách sản phẩm theo bố cục tham khảo, với thiết kế gọn và đáp ứng màn hình nhỏ.

Thay đổi:
- Gọi API `GET /api/products` để tải, tìm kiếm và phân trang dữ liệu thật.
- Thêm chọn số dòng/trang có tùy chỉnh, bảng sản phẩm, trạng thái tải/lỗi/rỗng và điều hướng trang.
- Thêm tải file CSV mẫu chạy phía FE; các thao tác thêm, sửa, xóa và upload hiện báo chưa có API.
- Thay trang khởi động bằng màn hình Master sản phẩm và đưa nội dung giao diện vào i18n.

File và thư mục bị ảnh hưởng:
- `FE/src/features/productMaster`
- `FE/src/pages/ProductMasterPage.tsx`
- `FE/src/App.tsx`
- `FE/src/i18n/locales.ts`
- `FE/src/shared/styles/global.css`
- `CHANGELOG.md`

Kiểm tra: `npm run build` thành công; giao diện tải được 10 sản phẩm qua API trên trình duyệt. Vite báo bundle JS lớn hơn 500 kB sau minify.


## 2026-11-02 - Hỗ trợ dấu phẩy thập phân khi tìm sản phẩm

Mục đích: Cho phép nhập số thập phân theo cả định dạng `0,6` và `0.6`.

Thay đổi:
- Chuẩn hóa dấu phẩy thành dấu chấm trước khi parse số.
- Từ chối giá trị trộn cả dấu phẩy và dấu chấm để tránh diễn giải sai.

File và thư mục bị ảnh hưởng:
- `BE/Services/ProductService.cs`
- `CHANGELOG.md`

Kiểm tra: `dotnet build` thành công với output tạm trong `BE/obj/compile-check`; không chạy test.


## 2026-11-02 - Cho phép tìm kiếm với từ khóa trống

Mục đích: Không trả lỗi khi người dùng chọn trường tìm kiếm nhưng chưa nhập nội dung.

Thay đổi:
- Coi `searchValue` rỗng hoặc chỉ chứa khoảng trắng là không lọc, vẫn trả danh sách theo phân trang.
- Chỉ trả lỗi khi có `searchValue` nhưng không gửi `searchField` tương ứng.

File và thư mục bị ảnh hưởng:
- `BE/Services/ProductService.cs`
- `CHANGELOG.md`

Kiểm tra: `dotnet build` thành công với output tạm trong `BE/obj/compile-check`; không chạy test.


## 2026-11-02 - Thêm API tìm kiếm và phân trang sản phẩm

Mục đích: Cung cấp API tải sản phẩm và tìm theo trường được chọn với kích thước trang do client gửi.

Thay đổi:
- Thêm `GET /api/products` qua Controller, Service và Repository dùng Dapper.
- Hỗ trợ tìm gần đúng theo mã, tên, đơn vị, quy cách; tìm chính xác theo số lượng/thùng và trọng lượng.
- Trả toàn bộ thuộc tính sản phẩm ngoại trừ `Id`, kèm tổng số dòng và thông tin phân trang.
- Yêu cầu `page` và `pageSize`; giới hạn `pageSize` từ 1 đến 200; không thêm migration vì chỉ đọc dữ liệu.

File và thư mục bị ảnh hưởng:
- `BE/Controllers/ProductsController.cs`
- `BE/DTOs/Products`
- `BE/Models/ProductSearchCriteria.cs`
- `BE/Repositories/ProductRepository.cs`
- `BE/Repositories/IProductRepository.cs`
- `BE/BE.http`
- `BE/Services/ProductService.cs`
- `BE/Services/IProductService.cs`
- `BE/Services/ProductSearchValidationException.cs`
- `BE/Program.cs`
- `CHANGELOG.md`

Kiểm tra: `dotnet build` thành công với output tạm trong `BE/obj/compile-check`; chưa gọi endpoint với SQL Server đang chạy.


## 2026-11-02 - FE kiểm tra kết nối database qua API thật

Mục đích: Dùng trang frontend gọi endpoint ping của `DatabaseController` để kiểm tra kết nối SQL Server.

Thay đổi:
- Gọi `GET /api/database/ping` và đọc phản hồi `connected`, `database`.
- Cập nhật trạng thái và nội dung giao diện để hiển thị kết quả kết nối database.
- Thêm Vite proxy cho `/api` đến backend `localhost:5069`, tránh yêu cầu CORS khi chạy FE ở chế độ phát triển.
- Để `VITE_API_BASE_URL` trống theo mặc định để request dùng proxy cùng origin.

File và thư mục bị ảnh hưởng:
- `FE/src/features/apiHealth`
- `FE/src/shared/api/httpClient.ts`
- `FE/src/i18n/locales.ts`
- `FE/vite.config.ts`
- `FE/.env`
- `FE/.env.example`
- `CHANGELOG.md`

Kiểm tra: Đã chạy `npm run build` thành công; chưa kiểm tra API khi backend và SQL Server đang chạy.


## 2026-11-02 - Sửa cấu hình API và khai báo kiểu Vite cho FE

Mục đích: Sửa đường dẫn biến môi trường API và lỗi import CSS trong `main.tsx`.

Thay đổi:
- Đổi tên file cấu hình `FE/env` thành `FE/.env` để Vite tự nạp; giữ URL backend `http://localhost:5069` theo launch profile.
- Đồng bộ URL mặc định trong `httpClient.ts` với cổng backend `5069`.
- Thêm `src/vite-env.d.ts` để TypeScript nhận kiểu `import.meta.env` và import tài nguyên CSS của Vite.
- Thêm file mẫu `.env.example` và cập nhật hướng dẫn cổng API trong README.

File và thư mục bị ảnh hưởng:
- `FE/.env`
- `FE/.env.example`
- `FE/src/vite-env.d.ts`
- `FE/src/shared/api/httpClient.ts`
- `README.md`
- `CHANGELOG.md`

Kiểm tra: Đã đối chiếu URL với cấu hình backend; chưa chạy build hoặc test.


## 2026-11-02 - Việt hóa tài liệu Markdown

Mục đích: Chuyển nội dung diễn giải trong các tài liệu Markdown của dự án sang tiếng Việt.

Thay đổi:
- Dịch nội dung hướng dẫn, kiến trúc, nhật ký migration và mô tả của coding skill; giữ nguyên tên file, đường dẫn, lệnh và định danh kỹ thuật.

File và thư mục bị ảnh hưởng:
- Các file `*.md` trong dự án, ngoài thư mục phụ thuộc và thư mục build.
- `CHANGELOG.md`

Kiểm tra: Đã rà soát các tài liệu Markdown; không chạy test vì chỉ thay đổi tài liệu.


## 2026-11-02 - Sắp xếp FE theo tính năng

Mục đích: Sắp xếp frontend React để mỗi phần tính năng có trách nhiệm rõ ràng.

Thay đổi:
- Chuyển màn hình kiểm tra API vào `pages` và `features/apiHealth`.
- Thêm Redux store, typed hooks, HTTP client dùng chung, tài nguyên i18n và app provider.
- Thêm hướng dẫn kiến trúc frontend và chuyển global styles vào `shared/styles`.

File và thư mục bị ảnh hưởng:
- `FE/src/app`
- `FE/src/features/apiHealth`
- `FE/src/pages`
- `FE/src/shared`
- `FE/src/i18n`
- `FE/ARCHITECTURE.md`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy theo hướng dẫn của workspace.


## Mẫu ghi lịch sử

```markdown

## YYYY-MM-DD - Tên thay đổi ngắn gọn

Mục đích: Kết quả theo yêu cầu của người dùng.

Thay đổi:
- Mô tả rõ từng thay đổi chính.

File và thư mục bị ảnh hưởng:
- `đường/dẫn/tương/đối`

Kiểm tra: Nêu nội dung đã kiểm tra, hoặc ghi `Chưa chạy` nếu người dùng không yêu cầu chạy test hay kiểm tra lúc chạy.
```


## 2026-11-02 - Thêm quy ước lưu lịch sử thay đổi

Mục đích: Lưu lại lịch sử dễ đọc mỗi khi prompt làm thay đổi dự án.

Thay đổi:
- Thêm file lịch sử ở thư mục gốc và quy tắc workspace yêu cầu ghi lại các thay đổi code, cấu hình, migration và kiến trúc sau này.

File và thư mục bị ảnh hưởng:
- `AGENTS.md`
- `CHANGELOG.md`

Kiểm tra: Chưa chạy.


## 2026-11-02 - Thêm kết nối database bằng Dapper

Mục đích: Kết nối backend với SQL Server bằng Dapper.

Thay đổi:
- Thêm connection string `DefaultConnection` cho môi trường phát triển và database `SmallProject`.
- Thêm API endpoint để kiểm tra database SQL Server đang kết nối.

File và thư mục bị ảnh hưởng:
- `BE/Program.cs`
- `BE/appsettings.Development.json`
- `BE/Controllers/DatabaseController.cs`
- `BE/Services/DatabaseService.cs`

Kiểm tra: Chưa chạy.


## 2026-11-02 - Thêm migration database và dữ liệu mẫu

Mục đích: Tạo schema `SmallProject` và dữ liệu mẫu cho môi trường phát triển.

Thay đổi:
- Thêm SQL migration tạo `MasterProduct`, `SaleOut`, bảng theo dõi migration và mười dòng dữ liệu mẫu cho mỗi bảng.

File và thư mục bị ảnh hưởng:
- `BE/Migrations/202611020001_InitialCreate.sql`
- `BE/Migrations/202611020002_SeedSampleData.sql`
- `BE/Migrations/MIGRATION_LOG.md`

Kiểm tra: Chưa chạy.
