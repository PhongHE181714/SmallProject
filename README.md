# SmallProject — Hướng dẫn chạy dự án

Dự án gồm Frontend và hệ thống Backend Microservices:

| Phần | Công nghệ | Cổng mặc định |
|------|-----------|---------------|
| **ApiGateway** — Cổng điều phối API | ASP.NET Core .NET 8 Reverse Proxy Gateway | `http://localhost:5069` |
| **Catalog.API** — Microservice Sản phẩm | ASP.NET Core .NET 8, DDD, CQRS, Clean Architecture, Dapper, RabbitMQ | `http://localhost:5071` |
| **Ordering.API** — Microservice Xuất hàng | ASP.NET Core .NET 8, DDD, CQRS, Clean Architecture, Dapper, RabbitMQ | `http://localhost:5072` |
| **FE** — Giao diện | React 18, TypeScript, Vite, Redux, MUI | `http://localhost:5173` |

> Chi tiết cấu trúc phân tầng `Controllers`, `Application`, `Domain`, `Repositories`, `ApiGateway` và luồng sự kiện RabbitMQ xem tại [BE/ARCHITECTURE.md](BE/ARCHITECTURE.md).

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

```text
BE/Migrations/202611020001_InitialCreate.sql
BE/Migrations/202611020002_SeedSampleData.sql
BE/Migrations/202611030001_AddIsActiveToMasterProductAndSaleOut.sql
BE/Migrations/202611030002_AddUniqueMasterProductFields.sql
BE/Migrations/202611040001_AddUniqueSaleOutCustomerPoProduct.sql
BE/Migrations/202611040002_RecalculateSaleOutBoxQuantity.sql
BE/Migrations/202611060001_AddSaleOutNo.sql
BE/Migrations/202611060002_BackfillSaleOutNo.sql
BE/Migrations/202611060003_GroupSaleOutProductsByOrder.sql
BE/Migrations/202611080001_CreateFnSaleOutReport.sql
```

Các script có kiểm tra `IF NOT EXISTS` — chạy lại vẫn an toàn, không tạo trùng.

---

## 2. Cấu hình Backend (BE)

Tạo hoặc cập nhật file **`appsettings.Development.json`** trong cả 2 service:
- `BE/Catalog.API/appsettings.Development.json`
- `BE/Ordering.API/appsettings.Development.json`

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

### Chạy các Microservices và API Gateway

```powershell
# Terminal 1 — Catalog.API (http://localhost:5071)
cd BE/Catalog.API
dotnet run

# Terminal 2 — Ordering.API (http://localhost:5072)
cd BE/Ordering.API
dotnet run

# Terminal 3 — ApiGateway (http://localhost:5069)
cd BE/ApiGateway
dotnet run
```

API Gateway chạy tại `http://localhost:5069` và tự động điều phối request từ Frontend sang `Catalog.API` (`:5071`) và `Ordering.API` (`:5072`).

---

## 3. Cấu hình Frontend (FE)

Tạo file **`FE/.env`** (file này không có trong repo):

```env
VITE_API_BASE_URL=http://localhost:5069
```

### Chạy FE

```powershell
cd FE
npm install
npm run dev
```

Mở trình duyệt tại `http://localhost:5173`.
