# SmallProject — Hướng dẫn chạy dự án

Dự án gồm hai phần:

| Phần | Công nghệ | Cổng mặc định |
|------|-----------|---------------|
| **BE** — API backend | ASP.NET Core .NET 8, Dapper, SQL Server | `http://localhost:5069` |
| **FE** — Giao diện | React 18, TypeScript, Vite, Redux, MUI | `http://localhost:5173` |

---

## Yêu cầu cài đặt trước

| Phần mềm | Phiên bản tối thiểu | Kiểm tra |
|----------|-------------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 18.x | `node --version` |
| [SQL Server](https://www.microsoft.com/sql-server) | 2019 trở lên (Express được) | SQL Server Management Studio |

---

## 1. Chuẩn bị Database

Chạy **toàn bộ** các file migration theo thứ tự số trong SQL Server Management Studio (hoặc `sqlcmd`):

```
BE/Migrations/202611020001_InitialCreate.sql
BE/Migrations/202611020002_SeedSampleData.sql
BE/Migrations/202611030001_AddIsActiveToMasterProductAndSaleOut.sql
BE/Migrations/202611030002_AddUniqueMasterProductFields.sql
BE/Migrations/202611040001_AddUniqueSaleOutCustomerPoProduct.sql
BE/Migrations/202611040002_RecalculateSaleOutBoxQuantity.sql
BE/Migrations/202611060001_AddSaleOutNo.sql
BE/Migrations/202611060002_BackfillSaleOutNo.sql
BE/Migrations/202611060003_GroupSaleOutProductsByOrder.sql
```

Các script có kiểm tra `IF NOT EXISTS` — chạy lại vẫn an toàn, không tạo trùng.

---

## 2. Cấu hình Backend (BE)

Tạo file **`BE/appsettings.Development.json`** (file này không có trong repo vì chứa thông tin kết nối):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<TÊN_SERVER>;Database=SmallProject;uid=<USER>;pwd=<PASSWORD>;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

> **Thay thế:**
> - `<TÊN_SERVER>` → tên SQL Server instance của bạn, ví dụ `localhost\SQLEXPRESS` hoặc `localhost\MSSQLSERVER`
> - `<USER>` / `<PASSWORD>` → tài khoản SQL Server của bạn
>
> Nếu dùng **Windows Authentication**, thay toàn bộ chuỗi kết nối thành:
> ```
> Server=<TÊN_SERVER>;Database=SmallProject;Trusted_Connection=True;TrustServerCertificate=True;
> ```

### Chạy BE

```powershell
cd BE
dotnet restore
dotnet run
```

API chạy tại `http://localhost:5069` — mở `http://localhost:5069/swagger` để xem tài liệu API.

---

## 3. Cấu hình Frontend (FE)

Tạo file **`FE/.env`** (file này không có trong repo):

```env
VITE_API_BASE_URL=http://localhost:5069
```

> Nếu BE chạy trên cổng khác hoặc server khác, thay URL tương ứng.

### Chạy FE

```powershell
cd FE
npm install
npm run dev
```

Mở trình duyệt tại `http://localhost:5173`.

---

## Tóm tắt nhanh

```
# Terminal 1 — Backend
cd BE
# (tạo appsettings.Development.json trước)
dotnet restore
dotnet run

# Terminal 2 — Frontend
cd FE
# (tạo .env trước)
npm install
npm run dev
```

---

## Lưu ý

- Chạy BE **trước** khi mở FE để tránh lỗi kết nối API.
- Nếu SQL Server yêu cầu mã hóa TLS, thêm `;Encrypt=True;` vào connection string.
- Font **Times New Roman** cần được cài trên máy chạy BE để xuất PDF đúng định dạng (mặc định đã có trên Windows).
