# Bộ khởi tạo BE + FE

Bộ khởi tạo gồm API ASP.NET Core .NET 8 trong `BE` và giao diện React + TypeScript + Vite trong `FE`.

## Chạy API

```powershell
cd BE
dotnet restore
dotnet run
```

API chạy tại `http://localhost:5069` theo cấu hình `BE/Properties/launchSettings.json`.

## Chạy giao diện

```powershell
cd FE
npm install
npm run dev
```

Mở `http://localhost:5173`. FE đọc biến `VITE_API_BASE_URL` từ `FE/.env`; file mẫu nằm ở `FE/.env.example`.
