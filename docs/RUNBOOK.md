# SCEAMS – Runbook clone → chạy (Phase 135)

## Prerequisites

- .NET SDK 8.x theo `global.json`.
- SQL Server 2019+ (local/container) và database user có quyền tạo schema.
- Git, `dotnet-ef` 8.x và HTTPS development certificate.
- Node/npm chỉ cần nếu chạy Newman/Postman smoke tests.

Kiểm tra:

```bash
dotnet --info
dotnet tool install --global dotnet-ef --version 8.*
dotnet dev-certs https --trust
```

## Cấu hình an toàn

Không sửa hoặc commit secret vào `appsettings.json`. Dùng User Secrets:

```bash
dotnet user-secrets init --project SCEAMS.API/SCEAMS.API.csproj
dotnet user-secrets set --project SCEAMS.API/SCEAMS.API.csproj \
  "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=SCEAMS;User Id=sa;Password=<local-password>;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set --project SCEAMS.API/SCEAMS.API.csproj \
  "Jwt:SigningKey" "<random-secret-at-least-32-characters>"
dotnet user-secrets set --project SCEAMS.API/SCEAMS.API.csproj \
  "SeedData:AdminPassword" "<local-admin-password>"
dotnet user-secrets set --project SCEAMS.API/SCEAMS.API.csproj \
  "SeedData:StaffPassword" "<local-staff-password>"
dotnet user-secrets set --project SCEAMS.API/SCEAMS.API.csproj \
  "SeedData:OrganizerPassword" "<local-organizer-password>"
dotnet user-secrets set --project SCEAMS.API/SCEAMS.API.csproj \
  "SeedData:StudentPassword" "<local-student-password>"
```

Có thể thay bằng environment variables: `ConnectionStrings__DefaultConnection`,
`Jwt__SigningKey`, `NotificationGrpc__Address`, `AI__ApiKey` và
`SeedData__AdminPassword`, `SeedData__StaffPassword`,
`SeedData__OrganizerPassword`, `SeedData__StudentPassword`. AI provider chỉ bật khi có endpoint/model/key local; retrieval và
FAQ UI không được dùng secret production.

## Thứ tự chạy từ clone

```bash
git clone <repository-url>
cd Project_V2
dotnet restore SCEAMS.sln
dotnet build SCEAMS.sln
dotnet ef database update \
  --project SCEAMS.API/SCEAMS.API.csproj \
  --startup-project SCEAMS.API/SCEAMS.API.csproj

# Chỉ Development; tạo bốn role và fixture [DEMO]
dotnet run --project SCEAMS.API/SCEAMS.API.csproj -- --seed
```

Mở ba terminal, giữ đúng thứ tự để gRPC client kết nối được:

```bash
# Terminal 1 – NotificationService
dotnet run --project SCEAMS.NotificationService/SCEAMS.NotificationService.csproj --launch-profile https

# Terminal 2 – API
dotnet run --project SCEAMS.API/SCEAMS.API.csproj --launch-profile https

# Terminal 3 – MVC
dotnet run --project SCEAMS.MVC/SCEAMS.MVC.csproj --launch-profile https
```

## URL và tài khoản

| Thành phần | URL mặc định |
|---|---|
| API Swagger | `https://localhost:7069/swagger` |
| API HTTP | `http://localhost:5195` |
| MVC HTTPS | `https://localhost:7034` |
| MVC HTTP | `http://localhost:5206` |
| NotificationService gRPC | `https://localhost:7001` |
| MVC health/demo accounts (Development) | `/System/Health`, `/System/DemoAccounts` |

Tài khoản: `admin@sceams.edu.vn`, `staff@sceams.edu.vn`,
`organizer@sceams.edu.vn`, `student@sceams.edu.vn`. Mật khẩu chính là giá trị
local tương ứng của `SeedData:*`; không có mật khẩu nào được commit.

## Smoke test sau khi chạy

```bash
curl -k -f https://localhost:7069/api/health
curl -k -f https://localhost:7069/api/health/database
bash tools/security-audit.sh --source-only
bash tools/business-rule-regression.sh
bash tools/odata-content-negotiation.sh --run
```

Nếu dùng Newman, copy environment example rồi điền token sau khi login:

```bash
npx --yes newman run postman/SCEAMS.e2e-role-smoke.postman_collection.json \
  -e /tmp/sceams-e2e-local.json --insecure --bail
```

## Xử lý lỗi thường gặp

- API báo thiếu `DefaultConnection`/`Jwt:SigningKey`: kiểm tra User Secrets hoặc
  tên environment variable có dấu `__`.
- SQL Server không kết nối: kiểm tra port, login, `TrustServerCertificate` và
  chạy `dotnet ef migrations list`; không xóa database production.
- MVC báo timeout: chạy gRPC trước API, sau đó kiểm tra `ApiSettings:BaseUrl` và
  `NotificationGrpc:Address`.
- Certificate local không tin cậy: chạy `dotnet dev-certs https --trust` hoặc
  chỉ dùng `--insecure` cho smoke test local.
- Seed lỗi validation: bảo đảm bốn `SeedData:*` đều có giá trị mạnh; seed chỉ
  chạy ở `Development`.
