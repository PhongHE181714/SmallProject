---
name: vietnamese-coding-rules
description: Áp dụng quy tắc code tiếng Việt tạm thời của người dùng khi triển khai hoặc đánh giá code trong workspace này. Bao gồm cách đặt tên cho C#, JavaScript/TypeScript, React, SQL Server; code dễ bảo trì; kiểm tra dữ liệu, xử lý null, transaction, ghi log và tối ưu SQL.
---

# Quy tắc code tạm thời

Áp dụng trong workspace `E:\Work` khi làm việc với code, cho đến khi người dùng sửa hoặc gỡ các hướng dẫn này. Đây là quy tắc dựa trên ảnh `CODING RULE` người dùng gửi. Ưu tiên yêu cầu cụ thể của người dùng trong tác vụ hiện tại. Trước khi sửa, đọc cấu trúc dự án và làm theo kiến trúc, thư viện, quy ước sẵn có; không tự giả định API hoặc thêm yêu cầu ngoài phạm vi.

## Quy ước đặt tên

### C# / .NET

- Biến và tham số: `camelCase`, ví dụ `totalAmount`, `customerName`, `isAvailable`.
- Hằng số và biến `static`: `UPPER_SNAKE_CASE`, ví dụ `GROUP_ADMIN`, `DATE_FORMAT`.
- Class, method và property: `PascalCase`, ví dụ `CustomerInfo`, `GetCustomer`.

### JavaScript / TypeScript / React

- Biến và hàm: `camelCase`.
- Class, type, interface và component: `PascalCase`.
- Tên interface cho props: `<ComponentName>Props`, ví dụ `CustomerCardProps`.
- Custom hook: bắt đầu bằng `use` rồi đến tên `PascalCase`, ví dụ `useCustomerData`.
- Hằng số: `UPPER_SNAKE_CASE`.
- Property dùng theo kiểu đặt tên đang có của dự án/framework; không đổi tên API công khai chỉ để ép một kiểu viết.

### SQL Server

- Bảng: `PascalCase`, ưu tiên cấu trúc `<Nghiệp vụ><Chức năng><Phân cấp nếu cần>`, ví dụ `MasterCustomer`, `SaleOrder`, `StockInDetail`.
- Cột: `PascalCase`, ví dụ `CustomerCode`, `CustomerName`.
- Index: `INDEX_<Table>_<Column>`.
- Primary key: `PK_<Table>`.
- Foreign key: `FK_<Bảng chứa khóa ngoại>_<Bảng được tham chiếu>[_<Phân biệt>]`.
- View: `vw` + tên `PascalCase`, ví dụ `vwSaleInstructionDetail`.
- Table-valued function: `fn` + tên `PascalCase`; biến trong function dùng `camelCase`; parameter, table và column dùng `PascalCase`.
- Stored procedure: `sp` + tên `PascalCase`; biến dùng `camelCase`; parameter dùng `PascalCase`.
- Tôn trọng quy ước và công cụ migration sẵn có của dự án nếu chúng đã quy định cách đặt tên khác.

## Nguyên tắc triển khai

- Tuân thủ kiến trúc và luồng code hiện có của dự án; tận dụng `HD_Cores`, `Platform` hoặc thư viện nội bộ khi dự án thực sự có và đang dùng chúng.
- Dùng tên biến, hàm, class và hằng số rõ nghĩa. Tránh comment ở mọi dòng; chỉ thêm comment để giải thích ý định hoặc logic khó hiểu.
- Không dùng magic number/string: đặt thành hằng số có tên nếu giá trị có ý nghĩa nghiệp vụ hoặc được dùng lặp lại.
- Nếu cùng một khái niệm có nhiều biến với kiểu khác nhau và tên bị nhập nhằng, có thể thêm thông tin kiểu vào tên. Tên biến ngày kết thúc bằng `Date`, ngày giờ bằng `DateTime`, giờ bằng `Time` khi cách đó giúp rõ nghĩa.
- Tránh nhiều tầng `if/else`; kiểm tra điều kiện lỗi/ngoại lệ trước và kết thúc sớm bằng guard clause/early return/fail fast.
- DRY: gom logic thực sự dùng chung và định nghĩa constant/static value tại một nơi. Không tạo abstraction chỉ để loại bỏ vài dòng lặp nếu nó làm code khó đọc hơn.
- KISS: ưu tiên cách viết rõ ràng hơn biểu thức ngắn hoặc toán tử ba ngôi lồng nhau. Mỗi hàm nên có một trách nhiệm; mốc 5–50 dòng trong ảnh là tham khảo, không phải giới hạn cứng. Tránh truy cập property qua nhiều cấp khi có thể đặt trách nhiệm phù hợp hơn (Law of Demeter).
- YAGNI: chỉ triển khai chức năng được yêu cầu; không xây sẵn tính năng hoặc độ phức tạp cho giả định tương lai.
- Dùng comment khi cần giải thích lý do, ý nghĩa không hiển nhiên, hoặc logic phức tạp; ưu tiên làm code tự mô tả.

## Kiểm tra dữ liệu và độ an toàn

Trước khi hoàn tất thay đổi liên quan đến dữ liệu, xác định các mục phù hợp trong danh sách sau và xử lý theo yêu cầu nghiệp vụ:

- Có kiểm tra trường bắt buộc và định dạng hợp lệ không?
- Có phát hiện dữ liệu trùng trong file/import và trùng đã có trên hệ thống không?
- Có xác thực khóa ngoại và giá trị thuộc dictionary/danh mục tự định nghĩa không?
- Có xử lý `null`/optional đúng ở mọi đường đi không? Với phép cộng/trừ/nhân/chia SQL, xác định rõ null phải giữ nguyên hay dùng giá trị mặc định; chỉ dùng `0` khi đúng nghiệp vụ.
- Trước khi truy cập navigation/foreign-key object, kiểm tra khả năng null và đảm bảo truy vấn đã include/load dữ liệu cần dùng.
- Lưu log exception theo cơ chế hiện có; không nuốt lỗi hoặc ghi thông tin nhạy cảm không cần thiết.
- Nếu một thao tác ghi nhiều bảng, dùng transaction phù hợp để tránh lưu dở dang.
- Nếu thay đổi cấu trúc database, cập nhật và lưu script/migration theo quy trình dự án.
- Sau khi merge hoặc tích hợp code, xem lại logic và ảnh hưởng giữa các nhánh thay đổi.
- Tránh lấy hoặc đọc lặp dữ liệu. Chỉ truy vấn phạm vi cần thiết; xem xét tối ưu khi phù hợp với dữ liệu và kiến trúc hiện có.

## Truy vấn SQL

- Dùng `UNION ALL` khi cần nối các tập kết quả cùng cấu trúc và không cần loại dòng trùng. Dùng `UNION` nếu yêu cầu nghiệp vụ cần loại trùng.
- Chỉ dùng `JOIN` khi cần kết hợp cột theo quan hệ giữa các bảng; ưu tiên join đúng khóa, thường là dữ liệu master khi phù hợp nghiệp vụ. Không thay `JOIN` bằng `UNION ALL` nếu hai thao tác mang ý nghĩa khác nhau.
- Khi join, kiểm tra điều kiện join dựa trên khóa/unique key phù hợp và xem xét index hiện có hoặc cần thiết.
- Chỉ lấy cột và khoảng thời gian cần dùng; tránh truy vấn dư thừa hoặc đọc đi đọc lại cùng dữ liệu.

## Quy trình khi sửa code

1. Đọc yêu cầu hiện tại và xác định phạm vi thay đổi.
2. Kiểm tra cấu trúc, quy ước, kiến trúc và cách xử lý lỗi/dữ liệu có sẵn ở vùng code liên quan.
3. Thực hiện thay đổi tối thiểu cần thiết, áp dụng quy tắc trên mà không phá vỡ tương thích hiện có.
4. Tự rà soát checklist liên quan: naming, trách nhiệm hàm, nhánh lỗi, null, validation, SQL, transaction/logging/migration nếu có.
5. Báo rõ file đã đổi và kiểm tra nào đã hoặc chưa chạy. Không tự chạy test/lệnh kiểm chứng nếu người dùng chưa yêu cầu.
