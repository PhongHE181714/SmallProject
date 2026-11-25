# Kiến trúc Backend: API Gateway + Microservices (Độc lập từng API) + DDD + CQRS + Clean Architecture + RabbitMQ

Backend được chia thành các dự án (`.csproj`) độc lập trong [SmallProject.sln](SmallProject.sln):
- **API Gateway (`ApiGateway.csproj` - Cổng `5069`)**: Điểm vào duy nhất cho Frontend (`FE`), điều phối (Reverse Proxy) các request HTTP sang từng Microservice API theo bảng định tuyến.
- **Catalog Microservice (`Catalog.API.csproj` - Cổng `5071`)**: Web API độc lập quản lý nghiệp vụ Sản phẩm (`/api/products`) và kiểm tra kết nối Database (`/api/database/ping`).
- **Ordering Microservice (`Ordering.API.csproj` - Cổng `5072`)**: Web API độc lập quản lý nghiệp vụ Xuất hàng (`/api/sale-outs`).
- **BuildingBlocks (`BuildingBlocks.csproj`)**: Thư viện nền tảng dùng chung chứa `SharedKernel` (DDD), `CQRS` (`ISender`/`Mediator`), `EventBus/RabbitMQ`, `ApiExceptionHandlingMiddleware` và `MicroserviceHostExtensions` (tự động quét Assembly để đăng ký DI).

---

## 1. Luồng xử lý Request qua API Gateway & Microservices

```text
Frontend (http://localhost:5173)
   │
   ▼
API Gateway (http://localhost:5069) [ApiGatewayProxyMiddleware]
   ├── /api/products/** ──► Catalog.API  (http://localhost:5071)
   ├── /api/database/** ──► Catalog.API  (http://localhost:5071)
   └── /api/sale-outs/** ─► Ordering.API (http://localhost:5072)
```

Bên trong mỗi Microservice API (`Catalog.API` và `Ordering.API`):
- **Thin Controller**: Mỗi action chỉ gồm 1–2 dòng gọi `_sender.SendCommandAsync(...)` hoặc `_sender.SendQueryAsync(...)`, hoàn toàn không chứa `try/catch` hay logic kiểm tra dữ liệu.
- **`ApiExceptionHandlingMiddleware`**: Tự động bắt các ngoại lệ từ tầng Application/Domain (`ArgumentException` $\rightarrow$ `400`, `KeyNotFoundException` $\rightarrow$ `404`, `InvalidOperationException` $\rightarrow$ `409`, `InvalidDataException` $\rightarrow$ `400/409` kèm danh sách lỗi dòng import, `PayloadTooLargeException` $\rightarrow$ `413`) và trả về chuẩn `ProblemDetails`.
- **`Program.cs` rút gọn tối đa & Tự động quét DI (Assembly Scanning)**:
  Nhờ [MicroserviceHostExtensions.cs](BuildingBlocks/Extensions/MicroserviceHostExtensions.cs), `Program.cs` của mỗi Microservice chỉ cần gọi `builder.Services.AddMicroserviceHost(builder.Configuration, typeof(Program).Assembly);` và `app.UseMicroserviceHost();`. Khi thêm mới bất kỳ `CommandHandler`, `QueryHandler`, `Consumer`, `Repository` hay `Service` nào trong tương lai, hệ thống tự động quét Assembly để đăng ký DI mà không cần khai báo thủ công.

---

## 2. Cấu trúc thư mục toàn bộ `BE/`

```text
BE/
├── SmallProject.sln                        # Solution quản lý toàn bộ các project
├── NuGet.Config                            # Cấu hình dùng kho package cục bộ .nuget/packages
├── BuildingBlocks/
│   ├── BuildingBlocks.csproj               # Class Library dùng chung cho mọi Microservice
│   ├── SharedKernel/                       # Entity, AggregateRoot, ValueObject, IDomainEvent, PagedResult, ProblemExceptionFactory, PayloadTooLargeException
│   ├── CQRS/                               # ICommand, ICommandHandler, IQuery, IQueryHandler, Unit, ISender, Mediator
│   ├── EventBus/
│   │   ├── Abstractions/                   # IntegrationEvent, IEventBus, IIntegrationEventHandler
│   │   ├── Events/                         # ProductUpsertedIntegrationEvent, ProductDeletedIntegrationEvent
│   │   └── RabbitMQ/                       # RabbitMqOptions, RabbitMqEventBus, RabbitMqBackgroundConsumer, EventBusConsumerController
│   ├── Data/                               # ISqlConnectionFactory, SqlConnectionFactory, SqlLikePattern
│   ├── Middlewares/
│   │   └── ApiExceptionHandlingMiddleware.cs # Tập trung xử lý lỗi trả về ProblemDetails để Controller luôn mỏng
│   └── Extensions/
│       └── MicroserviceHostExtensions.cs   # Tự động quét Assembly đăng ký Handlers/Repositories/Services & cấu hình Pipeline
│
├── ApiGateway/
│   ├── ApiGateway.csproj                   # Web API Gateway chạy tại http://localhost:5069
│   ├── Program.cs                          # Khởi tạo ngắn gọn bằng AddApiGateway() & UseApiGateway()
│   ├── appsettings.json                    # Bảng định tuyến (Routes) tới Catalog.API (:5071) & Ordering.API (:5072)
│   ├── Configuration/
│   │   └── GatewayOptions.cs               # Mô hình cấu hình Route của Gateway
│   ├── Middleware/
│   │   └── ApiGatewayProxyMiddleware.cs    # Reverse Proxy chuyển tiếp Request/Response/Stream/File tới Microservice đích
│   └── Extensions/
│       └── ApiGatewayHostExtensions.cs     # Hàm mở rộng cấu hình DI và Middleware cho Gateway
│
├── Catalog.API/
│   ├── Catalog.API.csproj                  # Web API độc lập cho Sản phẩm chạy tại http://localhost:5071
│   ├── Program.cs                          # Khởi tạo ngắn gọn bằng AddMicroserviceHost() & UseMicroserviceHost()
│   ├── Controllers/
│   │   ├── ProductsController.cs           # Thin Controller (/api/products)
│   │   └── DatabaseController.cs           # Thin Controller (/api/database/ping)
│   ├── Application/                        # Commands, Queries, DTOs, ProductSpreadsheetService
│   ├── Domain/                             # MasterProduct Aggregate, ProductSearchCriteria, DomainEvents, IProductRepository
│   └── Repositories/                       # ProductRepository (Commands, Query, Import), DatabaseRepository
│
├── Ordering.API/
│   ├── Ordering.API.csproj                 # Web API độc lập cho Xuất hàng chạy tại http://localhost:5072
│   ├── Program.cs                          # Khởi tạo ngắn gọn bằng AddMicroserviceHost() & UseMicroserviceHost()
│   ├── Controllers/
│   │   └── SaleOutsController.cs           # Thin Controller (/api/sale-outs)
│   ├── Application/                        # Commands, Queries, Consumers (RabbitMQ), DTOs, SaleOutDocumentService
│   ├── Domain/                             # SaleOutOrderAggregate, SaleOut, CustomerPurchaseOrder, ProductSnapshot, ISaleOutRepository
│   └── Repositories/                       # SaleOutRepository (Commands, Query), ProductSnapshotRepository
│
└── Migrations/                             # Các script SQL khởi tạo bảng, index, trigger, function
```
