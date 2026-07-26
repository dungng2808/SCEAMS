# SCEAMS - Roadmap triển khai theo phase

> Project: Student Club & Extracurricular Activity Management System  
> Backend: ASP.NET Core Web API  
> Client: ASP.NET Core MVC gọi Web API  
> Service phụ: ASP.NET Core gRPC  
> Database: SQL Server + Entity Framework Core  
> Cách làm: hoàn thành một chức năng nhỏ ở API, ngay phase kế tiếp làm MVC client để gọi và kiểm thử chức năng đó.

## 1. Quy tắc sử dụng roadmap

- [ ] Chỉ bắt đầu phase mới khi toàn bộ checkbox của phase hiện tại đã hoàn thành.
- [ ] Không viết business rule trong Controller; Controller chỉ nhận request, gọi Service và trả HTTP response.
- [ ] Toàn bộ hệ thống chỉ có đúng 3 project chạy: `SCEAMS.API`, `SCEAMS.MVC`, `SCEAMS.NotificationService`.
- [ ] Không tách `Domain`, `Application`, `Infrastructure` thành project/class library riêng; đây là các folder/namespace bên trong `SCEAMS.API`, giống `LibraryApi_4Layers`.
- [ ] Mỗi API phase phải có DTO, validation, authorization, HTTP status code và request kiểm thử trong Swagger/Postman.
- [ ] Mỗi MVC phase phải gọi API thật qua `HttpClient`; không truy cập trực tiếp `DbContext` của API.
- [ ] MVC lưu access token/refresh token ở server-side Session hoặc encrypted cookie và tự gắn `Authorization: Bearer <token>`.
- [ ] Mọi danh sách phải có trạng thái loading, empty, success và error.
- [ ] Mọi phase phải cập nhật Swagger/Postman request tương ứng trước khi được đánh dấu hoàn thành.
- [ ] Không đưa `PasswordHash`, refresh token thô hoặc dữ liệu nhạy cảm vào response DTO.
- [ ] Sau khi hoàn thành toàn bộ checkbox và kiểm thử của một phase, tự động tạo một Git commit riêng cho phase đó trước khi bắt đầu phase tiếp theo.
- [ ] Commit chỉ chứa các file thuộc phạm vi phase đang hoàn thành, không đưa thay đổi không liên quan của người dùng vào commit; message phải viết bằng tiếng Việt theo mẫu `phase NN: <mô tả ngắn bằng tiếng Việt>`.
- [ ] Sau khi commit thành công, tự động push lên `origin` của nhánh hiện tại; chỉ bắt đầu phase tiếp theo khi push thành công.

## 2. Kiến trúc đích

Trong roadmap này, yêu cầu “3 solution” được chuẩn hóa theo thuật ngữ .NET thành **một solution tổng chứa đúng 3 project**. Bốn layer của API là bốn folder trong cùng `SCEAMS.API.csproj`, đúng cách tổ chức của `LibraryApi_4Layers`.

```text
SCEAMS.sln
├── SCEAMS.API/                         # Project 1 - một ASP.NET Core Web API project
│   ├── SCEAMS.API.csproj
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── ApiControllerBase.cs
│   │   │   ├── AuthController.cs
│   │   │   ├── ClubsController.cs
│   │   │   ├── EventsController.cs
│   │   │   └── ...
│   │   └── Middleware/
│   ├── Application/
│   │   ├── Common/
│   │   │   ├── Result.cs
│   │   │   └── PagedResult.cs
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   │   ├── Services/
│   │   │   ├── Repositories/
│   │   │   ├── IGenericRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Mappings/
│   │   ├── Services/
│   │   └── Validators/
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Constants/
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── SceamsDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Migrations/
│   │   │   └── Seed/
│   │   ├── Repositories/
│   │   ├── UnitOfWork/
│   │   ├── Authentication/
│   │   ├── GrpcClients/
│   │   └── AI/
│   ├── Program.cs
│   └── appsettings.json
├── SCEAMS.MVC/                         # Project 2 - ASP.NET Core MVC client
│   ├── SCEAMS.MVC.csproj
│   ├── Controllers/
│   ├── Models/
│   ├── ViewModels/
│   ├── Services/
│   │   └── ApiClients/
│   ├── Handlers/
│   ├── Views/
│   ├── Program.cs
│   └── appsettings.json
├── SCEAMS.NotificationService/         # Project 3 - ASP.NET Core gRPC
│   ├── SCEAMS.NotificationService.csproj
│   ├── Protos/
│   ├── Services/
│   ├── Program.cs
│   └── appsettings.json
├── docs/
└── postman/
```

### Quy tắc Clean Architecture của `SCEAMS.API`

- [ ] `Domain` chỉ chứa entity, enum và hằng số nghiệp vụ; không tham chiếu EF Core, MVC, gRPC hoặc lớp ở layer khác.
- [ ] `Application` chứa DTO, `Result<T>`, interface Repository/Unit of Work/Service, AutoMapper profile, validator và business service.
- [ ] `Infrastructure` chứa `SceamsDbContext`, EF configuration, migration, repository implementation, Unit of Work, JWT implementation, gRPC client và AI provider.
- [ ] `Api` chỉ chứa Controller và middleware/filter liên quan HTTP.
- [ ] `Program.cs` là composition root: đăng ký DbContext, AutoMapper, repository, Unit of Work, service, JWT, OData, XML formatter và gRPC client.
- [ ] Controller kế thừa `ApiControllerBase`, gọi interface service và chuyển `Result`/`Result<T>` thành `IActionResult`.
- [ ] Controller không gọi trực tiếp `SceamsDbContext`, Repository, Unit of Work hoặc gRPC client.
- [ ] Application Service nhận `IUnitOfWork`, repository interface, mapper và external-service interface qua constructor.
- [ ] Repository chỉ xử lý truy vấn/persistence; business rule và chuyển trạng thái nằm trong Application Service.
- [ ] `IGenericRepository<T>` dùng cho thao tác chung; truy vấn đặc thù đặt trong `IClubRepository`, `IEventRepository`, `IRegistrationRepository`...
- [ ] Mọi thay đổi nhiều bảng hoặc cần concurrency phải đi qua `IUnitOfWork` và transaction.
- [ ] Vì bốn layer nằm chung một `.csproj`, ranh giới phụ thuộc được kiểm soát bằng namespace, code review và checklist này.

### Luồng thực hiện một chức năng API

```text
Domain entity/enum
    -> Application DTO + interface
    -> Infrastructure repository/external implementation
    -> Application service + Result<T>
    -> Api controller + ApiControllerBase
    -> Program.cs dependency injection
    -> Swagger/Postman
    -> MVC ApiClient + Controller + View
```

### Quy ước trạng thái

- [ ] `ClubStatus`: `PendingApproval`, `Approved`, `Rejected`, `Dissolved`.
- [ ] `ClubMembershipStatus`: `Pending`, `Active`, `Rejected`, `Removed`.
- [ ] `EventStatus`: `Draft`, `PendingApproval`, `Approved`, `Ongoing`, `Completed`, `Cancelled`, `Rejected`.
- [ ] `RegistrationStatus`: `Pending`, `Confirmed`, `Attended`, `CancelledByStudent`.
- [ ] Vai trò: `Admin`, `Staff`, `Organizer`, `Student`.

### Definition of Done áp dụng cho mọi cặp phase

- [ ] API build thành công và chạy đúng bằng Swagger/Postman.
- [ ] File của API nằm đúng một trong bốn layer; không tạo thêm project layer.
- [ ] Controller chỉ gọi Application Service và trả kết quả qua `ApiControllerBase`.
- [ ] MVC gọi đúng API, không dùng dữ liệu giả.
- [ ] Luồng thành công và ít nhất một luồng lỗi đã được kiểm thử.
- [ ] Quyền đúng trả `2xx`; thiếu đăng nhập trả `401`; sai role/sai ownership trả `403`.
- [ ] Validation sai trả `400`; không tìm thấy trả `404`; xung đột nghiệp vụ trả `409` khi phù hợp.
- [ ] Không còn exception chưa xử lý trong log.
- [ ] Phase đã được lưu thành một Git commit riêng và push lên remote sau khi tất cả kiểm thử đạt.

---

## Milestone A - Khởi tạo và nền tảng

### Phase 00 - Chốt phạm vi và công nghệ

- [x] Dùng .NET 8 để đồng nhất với `LibraryApi_4Layers`, trừ khi giảng viên yêu cầu phiên bản khác.
- [x] Chốt SQL Server, EF Core Code First, ASP.NET Core Identity hoặc `PasswordHasher`.
- [x] Chốt MVC server-rendered, Bootstrap và typed `HttpClient`.
- [x] Chốt giờ nghiệp vụ theo `Asia/Ho_Chi_Minh`, lưu thời gian trong database theo UTC.
- [x] Tạo `SCEAMS.sln` chứa đúng ba project: API, MVC và NotificationService.
- [x] Xác nhận API là một `.csproj` với bốn folder `Api/Application/Domain/Infrastructure`, không tạo bốn class library.
- [x] Tạo Git repository, `.gitignore`, `README.md` tối thiểu và nhánh làm việc.
- [x] Done khi `dotnet sln list` hiện đúng ba project và `dotnet build SCEAMS.sln` thành công.

### Phase 01 - API: Health check

- [x] Tạo `SCEAMS.API` và bốn folder layer giống `LibraryApi_4Layers`.
- [x] Tạo `Application/Common/Result.cs` và `Api/Controllers/ApiControllerBase.cs`.
- [x] Thêm `GET /api/health` trả tên service, version và trạng thái `Healthy`.
- [x] Đăng ký Swagger trong `Program.cs` và lưu request Postman kiểm tra `200 OK`.

### Phase 02 - MVC: Trang kiểm tra API

- [x] Tạo `SCEAMS.MVC`, cấu hình base URL của API bằng options.
- [x] Tạo trang `/System/Health` gọi `GET /api/health`.
- [x] Hiển thị rõ API online/offline và thông báo khi API không kết nối được.

### Phase 03 - API: Database migration đầu tiên

- [x] Tạo entity/enum trong `Domain`; không đặt Data Annotation phụ thuộc EF nếu có thể cấu hình bằng Fluent API.
- [x] Tạo `Infrastructure/Data/SceamsDbContext.cs` và EF configurations.
- [x] Tạo `Application/Interfaces/IGenericRepository.cs`, các repository interface đặc thù và `IUnitOfWork.cs`.
- [x] Tạo repository/Unit of Work implementation trong `Infrastructure`.
- [x] Tạo migration đầu tiên cho 10 entity: `User`, `ClubCategory`, `Club`, `ClubMembership`, `Venue`, `Event`, `Registration`, `Attendance`, `Feedback`, `ChatLog`.
- [x] Thêm `GET /api/health/database` kiểm tra kết nối SQL Server.
- [x] Đăng ký DbContext, repositories và Unit of Work trong `Program.cs`.
- [x] Done khi database được tạo bằng `dotnet ef database update`.

### Phase 04 - MVC: Trang kiểm tra database

- [x] Trang `/System/Health` gọi thêm `GET /api/health/database`.
- [x] Hiển thị riêng trạng thái API và database.
- [x] Kiểm thử một lần khi database chạy và một lần khi connection string sai.

### Phase 05 - API: Seed dữ liệu demo

- [x] Đặt seed code trong `Infrastructure/Data/Seed`.
- [x] Seed 4 tài khoản mẫu, category, club, venue, event và registration đủ để demo.
- [x] Hash mật khẩu; không ghi mật khẩu thô vào migration/log.
- [x] Seed phải idempotent và chạy lại không tạo dữ liệu trùng.

### Phase 06 - MVC: Kiểm tra tài khoản demo

- [x] Tạo trang development-only hiển thị email mẫu theo role, không hiển thị mật khẩu ở production.
- [x] Dùng trang health để xác nhận seed đã tồn tại.
- [x] Done khi có thể chuẩn bị dữ liệu demo từ database mới trong một lệnh.

---

## Milestone B - Authentication và hồ sơ cá nhân

### Phase 07 - API: Student đăng ký tài khoản

- [x] Thêm `POST /api/auth/register`.
- [x] Kiểm tra email và `StudentCode` không trùng; hash password; role mặc định là `Student`.
- [x] Trả `201 Created` với DTO an toàn; test email trùng và password không hợp lệ.

### Phase 08 - MVC: Màn hình đăng ký

- [x] Tạo form đăng ký Student với validation phía client và server.
- [x] Gọi API, hiển thị lỗi từng field và chuyển sang trang login khi thành công.
- [x] Kiểm thử đăng ký mới và đăng ký email trùng.

### Phase 09 - API: Đăng nhập và phát JWT

- [x] Thêm `POST /api/auth/login`.
- [x] Kiểm tra password hash và `IsActive`; trả access token, expiry và thông tin role.
- [x] JWT có claim `sub`, email, role; test sai password và tài khoản bị khóa.

### Phase 10 - MVC: Đăng nhập và lưu token

- [x] Tạo trang login gọi API và lưu token an toàn ở server-side Session/encrypted cookie.
- [x] Tạo `DelegatingHandler` tự gắn Bearer token vào mọi request API.
- [x] Điều hướng theo role và kiểm thử login bằng đủ 4 tài khoản mẫu.

### Phase 11 - API: Xem hồ sơ hiện tại

- [x] Thêm `GET /api/users/me`.
- [x] Lấy user ID từ JWT, chỉ trả hồ sơ của chính người đăng nhập.
- [x] Test token hợp lệ, thiếu token và user không còn tồn tại.

### Phase 12 - MVC: Trang hồ sơ cá nhân

- [x] Tạo `/Profile` gọi `GET /api/users/me`.
- [x] Hiển thị tên, email, student code, phone, role và trạng thái tài khoản.
- [x] Không cho người dùng thay đổi role hoặc trạng thái tài khoản từ trang này.

### Phase 13 - API: Cập nhật hồ sơ cá nhân

- [x] Thêm `PUT /api/users/me` chỉ cho sửa `FullName` và `PhoneNumber`.
- [x] Không nhận `Role`, `IsActive`, `PasswordHash` trong request DTO.
- [x] Test cập nhật thành công và số điện thoại không hợp lệ.

### Phase 14 - MVC: Sửa hồ sơ cá nhân

- [ ] Tạo form edit hồ sơ, pre-fill dữ liệu từ API.
- [ ] Gọi `PUT /api/users/me`, hiển thị validation và thông báo thành công.
- [ ] Tải lại hồ sơ để xác nhận dữ liệu thực sự đã lưu.

### Phase 15 - API: Đổi mật khẩu

- [ ] Thêm `PUT /api/users/me/password`.
- [ ] Bắt buộc mật khẩu hiện tại đúng; hash mật khẩu mới; vô hiệu hóa refresh token cũ.
- [ ] Test mật khẩu hiện tại sai và mật khẩu mới không đạt policy.

### Phase 16 - MVC: Form đổi mật khẩu

- [ ] Tạo form current/new/confirm password.
- [ ] Sau khi đổi thành công, xóa session token và yêu cầu đăng nhập lại.
- [ ] Xác nhận token cũ không còn dùng được.

### Phase 17 - API: Refresh token

- [ ] Thêm `POST /api/auth/refresh` và lưu refresh token dạng hash.
- [ ] Áp dụng rotation; token đã dùng hoặc đã revoke không được dùng lại.
- [ ] Test access token hết hạn và refresh token không hợp lệ.

### Phase 18 - MVC: Tự refresh và logout

- [ ] Khi API trả `401` do access token hết hạn, thử refresh đúng một lần rồi gửi lại request.
- [ ] Logout phải xóa session và gọi revoke token nếu API hỗ trợ.
- [ ] Nếu refresh thất bại, chuyển về login với return URL.

---

## Milestone C - Quản trị người dùng

### Phase 19 - API: Danh sách người dùng

- [ ] Thêm `GET /api/users` cho `Admin`.
- [ ] Hỗ trợ search, role filter, active filter, page và page size.
- [ ] Test Admin được xem, Staff/Organizer/Student nhận `403`.

### Phase 20 - MVC: Danh sách người dùng

- [ ] Tạo trang Admin Users có tìm kiếm, lọc và phân trang.
- [ ] Giữ query string khi chuyển trang.
- [ ] Xử lý empty state và `403 Forbidden`.

### Phase 21 - API: Admin tạo tài khoản

- [ ] Thêm `POST /api/users` cho `Admin`.
- [ ] Cho chọn role, trạng thái và mật khẩu ban đầu; vẫn phải hash password.
- [ ] Trả `201 Created`; test email trùng.

### Phase 22 - MVC: Admin tạo tài khoản

- [ ] Tạo form create user với danh sách role hợp lệ.
- [ ] Hiển thị validation từ API.
- [ ] Sau khi tạo, quay về danh sách và xác nhận user xuất hiện.

### Phase 23 - API: Admin sửa tài khoản

- [ ] Thêm `PUT /api/users/{id}` cho `Admin`.
- [ ] Chỉ sửa thông tin hồ sơ; chưa đổi role và trạng thái trong endpoint này.
- [ ] Test user không tồn tại và dữ liệu không hợp lệ.

### Phase 24 - MVC: Admin sửa tài khoản

- [ ] Tạo form edit riêng cho thông tin tài khoản.
- [ ] Không trộn thao tác role/lock vào form edit.
- [ ] Sau khi lưu, tải lại chi tiết để xác nhận.

### Phase 25 - API: Khóa hoặc mở khóa tài khoản

- [ ] Thêm `PUT /api/users/{id}/active-status`.
- [ ] Chặn Admin tự khóa tài khoản đang đăng nhập.
- [ ] Tài khoản bị khóa không thể login hoặc refresh token.

### Phase 26 - MVC: Nút khóa/mở khóa

- [ ] Thêm action có hộp xác nhận trên danh sách user.
- [ ] Cập nhật badge trạng thái sau khi API thành công.
- [ ] Hiển thị đúng lỗi khi Admin cố tự khóa chính mình.

### Phase 27 - API: Gán vai trò

- [ ] Thêm `PUT /api/users/{id}/role`.
- [ ] Chỉ chấp nhận 4 role đã định nghĩa; chặn tự hạ quyền Admin cuối cùng.
- [ ] Revoke refresh token sau khi đổi role.

### Phase 28 - MVC: Form gán vai trò

- [ ] Tạo thao tác đổi role riêng và có xác nhận.
- [ ] Sau khi đổi role, tải lại user và security badge.
- [ ] Kiểm thử user bị đổi role phải đăng nhập lại.

---

## Milestone D - Club Category

### Phase 29 - API: Xem danh mục CLB

- [ ] Thêm `GET /api/club-categories`.
- [ ] Cho phép public/authenticated đọc danh sách.
- [ ] Trả DTO gọn, sắp xếp theo tên.

### Phase 30 - MVC: Danh sách danh mục CLB

- [ ] Tạo trang danh mục dùng chung.
- [ ] Chỉ hiển thị nút quản trị cho Admin.
- [ ] Kiểm thử khi danh sách rỗng.

### Phase 31 - API: Tạo danh mục CLB

- [ ] Thêm `POST /api/club-categories` cho Admin.
- [ ] Tên category bắt buộc và không trùng không phân biệt hoa thường.
- [ ] Trả `201 Created`.

### Phase 32 - MVC: Tạo danh mục CLB

- [ ] Tạo form Admin create category.
- [ ] Hiển thị lỗi tên trùng.
- [ ] Xác nhận category mới xuất hiện trên danh sách.

### Phase 33 - API: Sửa danh mục CLB

- [ ] Thêm `PUT /api/club-categories/{id}` cho Admin.
- [ ] Kiểm tra tên trùng và category không tồn tại.
- [ ] Không làm thay đổi quan hệ Club hiện có.

### Phase 34 - MVC: Sửa danh mục CLB

- [ ] Tạo form edit category.
- [ ] Hiển thị lỗi `404` và `409` rõ ràng.
- [ ] Tải lại danh sách sau khi lưu.

### Phase 35 - API: Xóa danh mục CLB

- [ ] Thêm `DELETE /api/club-categories/{id}` cho Admin.
- [ ] Trả `409 Conflict` nếu category đang được Club sử dụng.
- [ ] Test xóa category rỗng và category đang được tham chiếu.

### Phase 36 - MVC: Xóa danh mục CLB

- [ ] Thêm hộp xác nhận xóa.
- [ ] Hiển thị lý do không thể xóa nếu đang được sử dụng.
- [ ] Không xóa row trên UI trước khi API trả thành công.

---

## Milestone E - Câu lạc bộ

### Phase 37 - API: Danh sách CLB có OData

- [ ] Thêm `GET /api/clubs` hỗ trợ `$filter`, `$orderby`, `$top`, `$skip`, `$select`, `$expand` phù hợp.
- [ ] Public chỉ thấy Club `Approved`; Admin/Staff có thể thấy theo status.
- [ ] Giới hạn `$top` để tránh truy vấn quá lớn.

### Phase 38 - MVC: Danh sách và tìm kiếm CLB

- [ ] Tạo trang danh sách với category filter, status filter theo quyền và phân trang.
- [ ] MVC tạo OData query an toàn, không ghép trực tiếp input chưa encode.
- [ ] Kiểm thử ví dụ `$filter=CategoryId eq ...&$orderby=Name`.

### Phase 39 - API: Chi tiết CLB

- [ ] Thêm `GET /api/clubs/{id}`.
- [ ] Trả thông tin category, organizer và số thành viên active; không lộ dữ liệu cá nhân không cần thiết.
- [ ] Public không xem được Club chưa Approved.

### Phase 40 - MVC: Trang chi tiết CLB

- [ ] Hiển thị thông tin CLB, category, organizer và số thành viên.
- [ ] Hiển thị action theo role và ownership.
- [ ] Kiểm thử truy cập Club pending bằng Student.

### Phase 41 - API: Organizer đề xuất thành lập CLB

- [ ] Thêm `POST /api/clubs` cho Organizer.
- [ ] Club mới luôn là `PendingApproval`, lấy `CreatedByUserId` từ JWT.
- [ ] Không cho client tự gửi status hoặc owner ID.

### Phase 42 - MVC: Form đề xuất CLB

- [ ] Tạo form Organizer chọn category và nhập thông tin CLB.
- [ ] Sau khi gửi, hiển thị status `PendingApproval`.
- [ ] Student không nhìn thấy menu tạo CLB.

### Phase 43 - API: Duyệt CLB

- [ ] Thêm `PUT /api/clubs/{id}/approve` cho Admin/Staff.
- [ ] Chỉ Club `PendingApproval` mới được duyệt.
- [ ] Ghi thời điểm/người duyệt nếu bổ sung audit fields.

### Phase 44 - MVC: Duyệt CLB

- [ ] Tạo danh sách Club chờ duyệt cho Admin/Staff.
- [ ] Thêm nút Approve có xác nhận.
- [ ] Sau khi duyệt, Club biến mất khỏi queue và public có thể xem.

### Phase 45 - API: Từ chối CLB

- [ ] Thêm `PUT /api/clubs/{id}/reject` với lý do bắt buộc.
- [ ] Chỉ từ `PendingApproval` sang `Rejected`.
- [ ] Organizer sở hữu Club có thể xem lý do từ chối.

### Phase 46 - MVC: Từ chối CLB

- [ ] Tạo modal nhập lý do từ chối.
- [ ] Hiển thị lý do cho Organizer ở trang Club của tôi.
- [ ] Không cho submit lý do rỗng.

### Phase 47 - API: Cập nhật thông tin CLB

- [ ] Thêm `PUT /api/clubs/{id}`.
- [ ] Admin được sửa mọi Club; Organizer chỉ sửa Club mình phụ trách.
- [ ] Không đổi owner/status bằng endpoint này.

### Phase 48 - MVC: Sửa thông tin CLB

- [ ] Tạo form edit chỉ hiện cho Admin hoặc Organizer sở hữu.
- [ ] Xử lý `403` khi ownership đã thay đổi.
- [ ] Tải lại chi tiết sau khi lưu.

### Phase 49 - API: Giải thể CLB

- [ ] Thêm `PUT /api/clubs/{id}/dissolve` cho Admin.
- [ ] Chuyển status sang `Dissolved`; không hard-delete lịch sử.
- [ ] Chặn tạo Event mới và đơn gia nhập mới cho Club đã giải thể.

### Phase 50 - MVC: Giải thể CLB

- [ ] Thêm action có cảnh báo ảnh hưởng.
- [ ] Hiển thị Club đã giải thể ở lịch sử nhưng không cho thao tác mới.
- [ ] Kiểm thử thử tạo Event sau khi giải thể phải thất bại.

---

## Milestone F - Thành viên CLB

### Phase 51 - API: Student xin gia nhập CLB

- [ ] Thêm `POST /api/clubs/{id}/members`.
- [ ] Tạo membership `Pending`; chặn đơn trùng đang Pending/Active.
- [ ] Chỉ nhận Student ID từ JWT.

### Phase 52 - MVC: Nút xin gia nhập CLB

- [ ] Thêm nút Join ở trang Club Approved.
- [ ] Sau khi gửi, nút chuyển sang `Đang chờ duyệt`.
- [ ] Hiển thị lỗi khi đã là thành viên.

### Phase 53 - API: Danh sách đơn gia nhập chờ duyệt

- [ ] Thêm `GET /api/clubs/{id}/members/pending`.
- [ ] Chỉ Admin hoặc Organizer phụ trách Club được xem.
- [ ] Hỗ trợ phân trang và search theo tên/student code.

### Phase 54 - MVC: Danh sách đơn gia nhập

- [ ] Tạo trang quản lý membership của Club.
- [ ] Hiển thị pending applications có phân trang.
- [ ] Kiểm thử Organizer khác Club nhận `403`.

### Phase 55 - API: Duyệt hoặc từ chối đơn gia nhập

- [ ] Thêm `PUT /api/clubs/{id}/members/{userId}/decision`.
- [ ] Quyết định `Approve` chuyển sang `Active`; `Reject` chuyển sang `Rejected`.
- [ ] Chỉ xử lý membership đang `Pending`.

### Phase 56 - MVC: Duyệt/từ chối đơn gia nhập

- [ ] Thêm hai action riêng với xác nhận.
- [ ] Cập nhật danh sách ngay sau khi API thành công.
- [ ] Hiển thị lỗi khi đơn đã được người khác xử lý.

### Phase 57 - API: Loại thành viên khỏi CLB

- [ ] Thêm `PUT /api/clubs/{id}/members/{userId}/remove`.
- [ ] Chuyển status sang `Removed`; không xóa lịch sử membership.
- [ ] Organizer chỉ thao tác trong Club mình phụ trách.

### Phase 58 - MVC: Loại thành viên

- [ ] Tạo danh sách thành viên Active.
- [ ] Thêm action Remove có lý do/xác nhận.
- [ ] Thành viên bị loại không còn xuất hiện trong danh sách Active.

---

## Milestone G - Địa điểm

### Phase 59 - API: Danh sách địa điểm

- [ ] Thêm `GET /api/venues` có search, maintenance filter và phân trang.
- [ ] Trả capacity và trạng thái bảo trì.
- [ ] Không trả navigation graph gây vòng lặp JSON/XML.

### Phase 60 - MVC: Danh sách địa điểm

- [ ] Tạo trang venue list.
- [ ] Hiển thị badge Available/Maintenance và capacity.
- [ ] Chỉ Admin/Staff thấy action quản trị.

### Phase 61 - API: Tạo địa điểm

- [ ] Thêm `POST /api/venues` cho Admin/Staff.
- [ ] Validate tên, location và capacity lớn hơn 0.
- [ ] Chặn venue trùng tên/location theo quy tắc đã chốt.

### Phase 62 - MVC: Tạo địa điểm

- [ ] Tạo form create venue.
- [ ] Hiển thị validation từ API.
- [ ] Xác nhận venue mới xuất hiện trong danh sách.

### Phase 63 - API: Cập nhật địa điểm

- [ ] Thêm `PUT /api/venues/{id}` cho Admin/Staff.
- [ ] Chỉ sửa tên, location, capacity; chưa thay maintenance ở endpoint này.
- [ ] Không cho giảm capacity thấp hơn số đăng ký hợp lệ của Event sắp tới nếu gây vi phạm.

### Phase 64 - MVC: Sửa địa điểm

- [ ] Tạo form edit venue.
- [ ] Hiển thị lỗi conflict khi capacity mới không hợp lệ.
- [ ] Tải lại chi tiết sau khi lưu.

### Phase 65 - API: Bật/tắt bảo trì địa điểm

- [ ] Thêm `PUT /api/venues/{id}/maintenance`.
- [ ] Chặn chuyển sang maintenance nếu có Event Approved/Ongoing bị ảnh hưởng, hoặc trả danh sách conflict.
- [ ] Chỉ Admin/Staff được thao tác.

### Phase 66 - MVC: Cập nhật bảo trì

- [ ] Tạo action maintenance riêng với cảnh báo.
- [ ] Hiển thị các Event xung đột nếu API trả `409`.
- [ ] Cập nhật badge sau khi thành công.

### Phase 67 - API: Xóa địa điểm

- [ ] Thêm `DELETE /api/venues/{id}` cho Admin.
- [ ] Chỉ hard-delete Venue chưa từng được Event tham chiếu; nếu đã dùng thì trả `409` và hướng dẫn maintenance.
- [ ] Test đủ hai trường hợp.

### Phase 68 - MVC: Xóa địa điểm

- [ ] Thêm action Delete chỉ cho Admin.
- [ ] Hiển thị đúng lý do không thể xóa.
- [ ] Không ẩn venue khỏi UI khi API thất bại.

### Phase 69 - API: Lịch sử dụng địa điểm

- [ ] Thêm `GET /api/venues/{id}/schedule`.
- [ ] Hỗ trợ khoảng ngày; trả các Event liên quan theo thời gian.
- [ ] Admin/Staff xem mọi status; role khác chỉ thấy Event public.

### Phase 70 - MVC: Lịch sử dụng địa điểm

- [ ] Tạo trang schedule theo date range.
- [ ] Hiển thị bảng hoặc calendar đơn giản.
- [ ] Kiểm thử venue không có lịch và venue có lịch trùng ngày.

---

## Milestone H - Sự kiện và workflow

### Phase 71 - API: Danh sách sự kiện có OData

- [ ] Thêm `GET /api/events` hỗ trợ `$filter`, `$orderby`, `$top`, `$skip`, `$select`, `$expand` phù hợp.
- [ ] Public/Student chỉ thấy Event `Approved` còn công khai; role nội bộ xem theo scope.
- [ ] Trả `slotsRemaining` được tính từ registration hợp lệ.

### Phase 72 - MVC: Danh sách sự kiện

- [ ] Tạo event list với keyword, category/club, date, status và còn chỗ.
- [ ] Gửi OData query đã encode.
- [ ] Kiểm thử ví dụ Event Approved theo `StartTime` tăng dần.

### Phase 73 - API: Chi tiết sự kiện

- [ ] Thêm `GET /api/events/{id}`.
- [ ] Trả Club, Venue, deadline, capacity, slots remaining và quyền thao tác hiện tại.
- [ ] Không public Event Draft/Pending của Organizer khác.

### Phase 74 - MVC: Trang chi tiết sự kiện

- [ ] Hiển thị đầy đủ thông tin và trạng thái.
- [ ] Chỉ hiện action hợp lệ theo role, ownership, deadline và status.
- [ ] Kiểm thử direct URL tới Event không có quyền.

### Phase 75 - API: Organizer tạo Event Draft

- [ ] Thêm `POST /api/events`.
- [ ] Club phải thuộc Organizer, Club đã Approved, Venue không maintenance.
- [ ] Validate `StartTime < EndTime`, deadline trước start, capacity hợp lệ; status luôn `Draft`.

### Phase 76 - MVC: Form tạo Event Draft

- [ ] Chỉ liệt kê Club Organizer phụ trách và Venue khả dụng.
- [ ] Tạo form ngày giờ/capacity/deadline.
- [ ] Sau khi thành công, chuyển tới chi tiết Event Draft.

### Phase 77 - API: Cập nhật Event

- [ ] Thêm `PUT /api/events/{id}`.
- [ ] Organizer chỉ sửa Event mình tạo ở trạng thái cho phép; Admin theo BR8.
- [ ] Không cho sửa thông tin chính khi Completed/Cancelled.

### Phase 78 - MVC: Form sửa Event

- [ ] Chỉ hiện nút Edit khi API cho phép.
- [ ] Hiển thị validation thời gian, capacity và ownership.
- [ ] Tải lại chi tiết sau khi lưu.

### Phase 79 - API: Gửi Event để duyệt

- [ ] Thêm `PUT /api/events/{id}/submit`.
- [ ] Chỉ chuyển `Draft -> PendingApproval`.
- [ ] Kiểm tra lại dữ liệu bắt buộc trước khi chuyển trạng thái.

### Phase 80 - MVC: Gửi Event để duyệt

- [ ] Thêm nút Submit for approval có xác nhận.
- [ ] Sau khi thành công, khóa các trường không còn được phép sửa.
- [ ] Hiển thị status `PendingApproval`.

### Phase 81 - API: Danh sách Event chờ duyệt

- [ ] Thêm `GET /api/events/pending-approval` cho Admin/Staff.
- [ ] Hỗ trợ lọc Club, Venue, date và phân trang.
- [ ] Chỉ trả `PendingApproval`.

### Phase 82 - MVC: Queue duyệt Event

- [ ] Tạo trang pending approval.
- [ ] Có link xem chi tiết trước khi quyết định.
- [ ] Giữ filter và page sau khi quay lại queue.

### Phase 83 - API: Duyệt Event và kiểm tra trùng lịch

- [ ] Thêm `PUT /api/events/{id}/approve` cho Admin/Staff.
- [ ] Kiểm tra Venue không maintenance và không overlap Event Approved/Ongoing khác.
- [ ] Chỉ chuyển `PendingApproval -> Approved`; conflict trả `409` kèm Event xung đột.

### Phase 84 - MVC: Duyệt Event

- [ ] Thêm action Approve trên queue/chi tiết.
- [ ] Khi conflict, hiển thị rõ Event, Venue và khung giờ bị trùng.
- [ ] Khi thành công, Event mở cho Student đăng ký.

### Phase 85 - API: Từ chối Event

- [ ] Thêm `PUT /api/events/{id}/reject` với lý do bắt buộc.
- [ ] Chỉ chuyển `PendingApproval -> Rejected`.
- [ ] Organizer sở hữu có thể xem lý do.

### Phase 86 - MVC: Từ chối Event

- [ ] Tạo modal nhập lý do.
- [ ] Hiển thị lý do ở trang Event của Organizer.
- [ ] Không cho gửi lý do rỗng.

### Phase 87 - API: Hủy Event

- [ ] Thêm `PUT /api/events/{id}/cancel`.
- [ ] Organizer chỉ hủy Event của mình trước StartTime; Admin được can thiệp theo BR8.
- [ ] Lưu lý do hủy và không hard-delete Event/Registration.

### Phase 88 - MVC: Hủy Event

- [ ] Tạo action Cancel có lý do và cảnh báo số người đã đăng ký.
- [ ] Hiển thị status/lý do hủy cho người liên quan.
- [ ] Kiểm thử Organizer hủy sau StartTime bị từ chối.

### Phase 89 - API: Đồng bộ trạng thái theo thời gian

- [ ] Tạo background job/service chuyển `Approved -> Ongoing -> Completed` theo StartTime/EndTime.
- [ ] Job idempotent, có log và không thay đổi Event Cancelled.
- [ ] Có endpoint Admin development-only hoặc test hook để chạy job khi demo.

### Phase 90 - MVC: Hiển thị trạng thái theo thời gian

- [ ] Trang Event phản ánh status mới sau khi job chạy.
- [ ] Action check-in chỉ hiện ở Ongoing; feedback chỉ hiện sau khi Attended/Completed.
- [ ] Demo bằng dữ liệu thời gian gần hoặc test clock.

---

## Milestone I - Đăng ký, điểm danh và phản hồi

### Phase 91 - API: Student đăng ký Event

- [ ] Thêm `POST /api/registrations`.
- [ ] Áp dụng BR1-BR3: Event Approved, chưa quá deadline, còn chỗ, không đăng ký trùng.
- [ ] Dùng transaction/concurrency control để không vượt capacity.

### Phase 92 - MVC: Nút đăng ký Event

- [ ] Thêm Register trên Event detail khi đủ điều kiện.
- [ ] Sau khi thành công, cập nhật slots remaining và trạng thái đăng ký.
- [ ] Hiển thị rõ lỗi hết chỗ, quá deadline và đăng ký trùng.

### Phase 93 - API: Student hủy đăng ký

- [ ] Thêm `PUT /api/registrations/{id}/cancel`.
- [ ] Chỉ chủ sở hữu được hủy; áp dụng mốc 24 giờ trước StartTime theo BR4.
- [ ] Lưu `CancelledAt`, không xóa registration.

### Phase 94 - MVC: Hủy đăng ký

- [ ] Thêm nút Cancel ở lịch sử/chi tiết khi còn được phép.
- [ ] Hiển thị deadline hủy và hộp xác nhận.
- [ ] Sau khi hủy, slots remaining tăng đúng.

### Phase 95 - API: Lịch sử đăng ký của Student

- [ ] Thêm `GET /api/registrations/me`.
- [ ] Lấy Student từ JWT; hỗ trợ status filter và phân trang.
- [ ] Không cho truyền StudentId tùy ý.

### Phase 96 - MVC: Lịch sử đăng ký

- [ ] Tạo trang My Registrations.
- [ ] Hiển thị Event, thời gian, registration status và attendance status.
- [ ] Từ đây có thể vào chi tiết hoặc hủy khi hợp lệ.

### Phase 97 - API: Danh sách người đăng ký của Event

- [ ] Thêm `GET /api/events/{id}/registrations`.
- [ ] Admin hoặc Organizer sở hữu mới được xem.
- [ ] Hỗ trợ status filter, search và phân trang.

### Phase 98 - MVC: Danh sách người đăng ký

- [ ] Tạo trang Event Registrations cho Organizer.
- [ ] Hiển thị Student code, tên và status; hạn chế dữ liệu cá nhân.
- [ ] Kiểm thử Organizer khác nhận `403`.

### Phase 99 - API: Điểm danh

- [ ] Thêm `PUT /api/registrations/{id}/check-in`.
- [ ] Organizer chỉ check-in Event mình phụ trách trong trạng thái/thời gian cho phép.
- [ ] Tạo tối đa một Attendance và chuyển Registration sang `Attended`.

### Phase 100 - MVC: Điểm danh

- [ ] Thêm action Check-in trong danh sách registrations.
- [ ] Chống double-click và hiển thị thời gian/người check-in.
- [ ] Kiểm thử check-in trùng không tạo Attendance thứ hai.

### Phase 101 - API: Student gửi Feedback

- [ ] Thêm `POST /api/events/{id}/feedback`.
- [ ] Áp dụng BR5: chỉ Student đã Attended, mỗi Student tối đa một feedback/Event.
- [ ] Validate rating 1-5 và giới hạn độ dài comment.

### Phase 102 - MVC: Form gửi Feedback

- [ ] Chỉ hiện form khi API xác nhận đủ điều kiện.
- [ ] Gửi rating và comment; hiển thị lỗi chưa Attended/trùng feedback.
- [ ] Sau khi gửi, khóa form và hiển thị feedback đã gửi.

### Phase 103 - API: Xem tổng hợp Feedback

- [ ] Thêm `GET /api/events/{id}/feedback`.
- [ ] Trả average rating, total feedback và danh sách phân trang.
- [ ] Không trả dữ liệu Student nhạy cảm.

### Phase 104 - MVC: Hiển thị đánh giá Event

- [ ] Hiển thị average rating và feedback list ở Event detail.
- [ ] Có empty state khi chưa có feedback.
- [ ] Xác nhận average cập nhật sau khi Student gửi feedback.

---

## Milestone J - Báo cáo và thống kê

### Phase 105 - API: Báo cáo Event theo trạng thái

- [ ] Thêm `GET /api/reports/event-summary` cho Admin/Staff.
- [ ] Trả số Event theo status và khoảng thời gian.
- [ ] Test tổng khớp với dữ liệu seed.

### Phase 106 - MVC: Dashboard Event summary

- [ ] Hiển thị bảng hoặc chart theo status.
- [ ] Có date range filter và total.
- [ ] Xử lý tập dữ liệu rỗng.

### Phase 107 - API: Báo cáo hoạt động CLB

- [ ] Thêm `GET /api/reports/club-activity`.
- [ ] Admin/Staff xem toàn bộ; Organizer chỉ Club mình.
- [ ] Trả số Event, registrations, attendance và average rating theo Club.

### Phase 108 - MVC: Dashboard hoạt động CLB

- [ ] Hiển thị bảng xếp hạng/top Club.
- [ ] Organizer chỉ thấy dữ liệu đúng scope.
- [ ] Kiểm thử filter date range.

### Phase 109 - API: Báo cáo tỷ lệ tham dự

- [ ] Thêm `GET /api/reports/attendance-rate`.
- [ ] Tính `Attended / Confirmed` theo Event, tránh chia cho 0.
- [ ] Admin/Staff xem toàn bộ; Organizer xem Event của mình.

### Phase 110 - MVC: Báo cáo tỷ lệ tham dự

- [ ] Hiển thị registered, attended và attendance rate.
- [ ] Có link tới danh sách registrations của Event.
- [ ] Kiểm thử Event chưa có registration.

### Phase 111 - API: Báo cáo sử dụng Venue

- [ ] Thêm `GET /api/reports/venue-usage`.
- [ ] Trả số Event và tổng số giờ sử dụng theo Venue/date range.
- [ ] Chỉ Admin/Staff truy cập.

### Phase 112 - MVC: Báo cáo sử dụng Venue

- [ ] Hiển thị bảng/chart Venue usage.
- [ ] Có date range filter.
- [ ] Từ report có link sang Venue schedule.

---

## Milestone K - Yêu cầu kỹ thuật bắt buộc

### Phase 113 - API: Content negotiation JSON/XML

- [ ] Đăng ký XML formatter.
- [ ] Áp dụng `[Produces("application/json", "application/xml")]` cho ít nhất `GET /api/events`.
- [ ] Test cùng endpoint với `Accept: application/json` và `Accept: application/xml`.
- [ ] Dùng response DTO để tránh vòng lặp serialization.

### Phase 114 - MVC: Trang demo JSON/XML

- [ ] Tạo trang kỹ thuật cho phép chọn JSON hoặc XML.
- [ ] MVC gửi đúng `Accept` header và hiển thị raw response đã escape an toàn.
- [ ] Hiển thị response `406 Not Acceptable` khi yêu cầu format không hỗ trợ.

### Phase 115 - API: Notification gRPC khi Event đổi trạng thái

- [ ] Đặt `.proto` và server implementation trong project `SCEAMS.NotificationService`.
- [ ] Đặt `INotificationClientService` trong `SCEAMS.API/Application/Interfaces`.
- [ ] Đặt `NotificationClientService` implementation trong `SCEAMS.API/Infrastructure/GrpcClients`.
- [ ] `EventService` chỉ gọi interface notification sau khi Event Approved hoặc Cancelled; Controller không gọi gRPC trực tiếp.
- [ ] Có timeout, retry giới hạn, correlation ID và log; không để retry tạo thông báo trùng.

### Phase 116 - MVC: Kiểm thử notification từ workflow

- [ ] Sau Approve/Cancel, MVC hiển thị kết quả nghiệp vụ và correlation ID nếu có.
- [ ] Tạo trang Admin development-only xem notification log giả lập.
- [ ] Demo được cả trường hợp gRPC chạy và gRPC tạm dừng.

### Phase 117 - API: Nhắc Event trước deadline 24 giờ

- [ ] Tạo background worker tìm Event sắp tới hạn đăng ký trong 24 giờ.
- [ ] Gọi gRPC một lần cho mỗi Event/loại thông báo; lưu dấu đã gửi.
- [ ] Job idempotent và có test clock.

### Phase 118 - MVC: Theo dõi reminder

- [ ] Trang Admin notification log hiển thị reminder đã gửi.
- [ ] Có filter theo Event/type/status.
- [ ] Demo chạy job hai lần nhưng không tạo reminder trùng.

### Phase 119 - API: ProblemDetails và global exception handling

- [ ] Đặt global exception handler trong `SCEAMS.API/Api/Middleware` và trả RFC-style `ProblemDetails`.
- [ ] Map validation, not found, forbidden, conflict và lỗi ngoài dự kiến.
- [ ] Không lộ stack trace/connection string ở production.

### Phase 120 - MVC: Xử lý lỗi API thống nhất

- [ ] Typed client parse `ProblemDetails`.
- [ ] Tạo trang/partial lỗi chung cho `400`, `401`, `403`, `404`, `409`, `500`.
- [ ] Kiểm thử return URL sau `401` và access-denied page sau `403`.

---

## Milestone L - AI FAQ Assistant (mở rộng theo tài liệu SCEAMS)

> Chỉ bắt đầu milestone này sau khi toàn bộ chức năng bắt buộc và gRPC đã ổn định.

### Phase 121 - API: Retrieval Event không dùng AI

- [ ] Tạo hàm parse keyword, `hôm nay`, `tuần này`, `tháng này` và điều kiện còn chỗ.
- [ ] Truy vấn tối đa 5-10 Event `Approved`, sắp xếp theo StartTime.
- [ ] Kiểm thử retrieval bằng dữ liệu cố định và test clock.

### Phase 122 - MVC: Trang kiểm thử retrieval

- [ ] Tạo trang Student nhập câu hỏi và xem Event retrieval được.
- [ ] Hiển thị title, club, time, venue và slots remaining.
- [ ] Xác nhận không có Event Draft/Pending trong kết quả.

### Phase 123 - API: Sinh câu trả lời bằng AI provider

- [ ] Thêm `POST /api/chatbot/ask`.
- [ ] Build context chỉ từ Event retrieval; không gửi dữ liệu nhạy cảm.
- [ ] Provider chỉ được trả lời dựa trên context; context rỗng phải trả “không tìm thấy”.
- [ ] Đặt AI provider interface trong `Application/Interfaces`, implementation trong `Infrastructure/AI`.
- [ ] `AIChatService` gọi provider interface; `ChatbotController` không gọi HTTP provider trực tiếp.

### Phase 124 - MVC: Giao diện AI FAQ

- [ ] Tạo giao diện hỏi-đáp đơn giản.
- [ ] Hiển thị `answer` và các `relatedEvents` có link tới chi tiết/đăng ký.
- [ ] Có loading, timeout, provider unavailable và empty-result state.

### Phase 125 - API: Lưu và xem lịch sử chatbot

- [ ] Lưu `Question`, `AnswerText`, `RelatedEventIds`, `StudentId`, `CreatedAt`.
- [ ] Thêm `GET /api/chatbot/history` lấy Student từ JWT và phân trang.
- [ ] Student chỉ xem log của chính mình.

### Phase 126 - MVC: Lịch sử chatbot

- [ ] Tạo trang My Chat History.
- [ ] Hiển thị câu hỏi, câu trả lời, thời gian và related Event.
- [ ] Kiểm thử Student A không thể xem lịch sử Student B.

### Phase 127 - API: Giới hạn 10 câu hỏi/giờ

- [ ] Áp dụng BR11 theo Student ID.
- [ ] Request vượt giới hạn trả `429 Too Many Requests` và `Retry-After`.
- [ ] Không gọi AI provider khi đã vượt giới hạn.

### Phase 128 - MVC: Hiển thị rate limit

- [ ] Parse `429` và hiển thị thời điểm được hỏi lại.
- [ ] Vô hiệu hóa form trong thời gian cần chờ.
- [ ] Kiểm thử bằng giới hạn thấp ở development.

---

## Milestone M - Hoàn thiện, kiểm thử và hồ sơ nộp

### Phase 129 - Security audit

- [ ] Lập security matrix đầy đủ cho Admin/Staff/Organizer/Student.
- [ ] Chạy bộ Postman/Newman cho toàn bộ endpoint với anonymous, đúng role, sai role và sai ownership.
- [ ] Kiểm tra mass assignment, IDOR, password hash leakage, token leakage và dữ liệu cá nhân.
- [ ] Kiểm tra CORS, HTTPS, secrets và production logging.

### Phase 130 - Business rule regression

- [ ] Test BR1: chỉ đăng ký Event Approved và chưa quá deadline.
- [ ] Test BR2: không vượt capacity kể cả request đồng thời.
- [ ] Test BR3: không đăng ký trùng.
- [ ] Test BR4: không hủy sau mốc cho phép.
- [ ] Test BR5: chỉ Attended mới feedback.
- [ ] Test BR6: Organizer chỉ thao tác Club/Event mình phụ trách.
- [ ] Test BR7: approve phải chặn trùng lịch Venue.
- [ ] Test BR8: Completed/Cancelled không sửa thông tin chính.
- [ ] Test BR9: membership chỉ Active sau khi được duyệt.
- [ ] Test BR10: chatbot không bịa Event ngoài retrieval.
- [ ] Test BR11: giới hạn câu hỏi AI.

### Phase 131 - Kiểm thử end-to-end theo vai trò

- [ ] Admin: login -> quản lý user/category/venue -> xem reports.
- [ ] Staff: login -> duyệt Club -> duyệt Event -> xem reports.
- [ ] Organizer: login -> đề xuất Club -> quản lý member -> tạo/submit Event -> check-in.
- [ ] Student: register/login -> join Club -> register/cancel Event -> xem history -> feedback.
- [ ] AI: Student hỏi -> retrieval -> answer -> history -> rate limit.
- [ ] gRPC: Approve/Cancel/reminder tạo notification đúng một lần.

### Phase 132 - OData và content negotiation demo

- [ ] Demo `GET /api/events?$filter=Status eq 'Approved'&$orderby=StartTime&$top=10`.
- [ ] Demo `$select` và `$expand` hợp lệ trên Events/Clubs.
- [ ] Demo cùng endpoint với JSON và XML.
- [ ] Lưu request/response mẫu vào `docs/` và Postman collection.

### Phase 133 - Dữ liệu demo và tài khoản mẫu

- [ ] Có ít nhất một tài khoản cho mỗi role.
- [ ] Có dữ liệu cho mọi trạng thái chính của Club, Event, Registration.
- [ ] Có dữ liệu tạo được venue conflict, capacity full, quá deadline và feedback hợp lệ.
- [ ] Tài khoản/mật khẩu demo được ghi trong README dành cho giảng viên, không dùng production.

### Phase 134 - Tài liệu kỹ thuật bắt buộc

- [ ] Giới thiệu, mục tiêu và phạm vi SCEAMS.
- [ ] Actor/use case theo 4 role.
- [ ] ERD có PK, FK, cardinality và migration/script.
- [ ] Danh sách business rules và workflow trạng thái.
- [ ] Sơ đồ một solution có đúng ba project; riêng API có bốn folder layer và trách nhiệm từng layer/service.
- [ ] Endpoint list gồm method, route, DTO, role và status code.
- [ ] Security matrix.
- [ ] OData demo; JSON/XML content negotiation demo.
- [ ] gRPC service và sequence Web API -> NotificationService.
- [ ] AI FAQ architecture, giới hạn và bảo vệ dữ liệu.

### Phase 135 - Hướng dẫn chạy

- [ ] Ghi prerequisites: .NET SDK, SQL Server và cấu hình cần thiết.
- [ ] Ghi lệnh restore, migration, seed, chạy gRPC, API và MVC theo đúng thứ tự.
- [ ] Dùng User Secrets/environment variables cho JWT key và AI API key.
- [ ] Ghi URL Swagger, MVC và tài khoản demo.
- [ ] Một người khác có thể clone và chạy theo README mà không cần hỏi thêm.

### Phase 136 - Gói sản phẩm nộp

- [ ] `dotnet sln list` chỉ có `SCEAMS.API`, `SCEAMS.MVC`, `SCEAMS.NotificationService`.
- [ ] Trong `SCEAMS.API` có đủ `Api`, `Application`, `Domain`, `Infrastructure`; không có project layer riêng.
- [ ] Source code sạch; không chứa `bin/`, `obj/`, secrets hoặc file database cá nhân.
- [ ] Migration hoặc SQL script và dữ liệu mẫu.
- [ ] ERD ảnh/PDF.
- [ ] Tài liệu project.
- [ ] Postman collection/environment.
- [ ] Mã nguồn MVC client.
- [ ] Mã nguồn gRPC NotificationService.
- [ ] File cấu hình mẫu `appsettings.Example.json`.
- [ ] `dotnet build SCEAMS.sln` và Postman/Newman collection pass từ clean checkout.

---

## 3. Checklist yêu cầu bắt buộc từ đề bài

- [ ] Project cá nhân, đề tài có ít nhất 3 role; SCEAMS có 4 role.
- [ ] Có ít nhất 5 entity; SCEAMS có 10 entity.
- [ ] Có quan hệ 1-n và n-n có thuộc tính.
- [ ] Có ít nhất một workflow trạng thái; SCEAMS có Club, Event và Registration workflow.
- [ ] Có ít nhất 5 business rules; roadmap kiểm thử 11 rules.
- [ ] SQL Server + EF Core + migration/script + seed.
- [ ] Kiến trúc nhiều tầng, Service Layer chứa business logic.
- [ ] RESTful API, DTO, status code đúng.
- [ ] Tìm kiếm, lọc, sắp xếp và phân trang.
- [ ] Tối thiểu 2 API hỗ trợ OData: Events và Clubs.
- [ ] Content negotiation JSON/XML.
- [ ] JWT authentication, password hashing và role authorization.
- [ ] User chỉ thao tác dữ liệu của chính mình; Organizer bị giới hạn theo Club ownership.
- [ ] ASP.NET Core MVC client có login, lưu JWT và gửi Bearer token.
- [ ] MVC có list, create/update và ít nhất một workflow nghiệp vụ.
- [ ] MVC xử lý `400`, `401`, `403`, `404`, `409` và `500`.
- [ ] Có gRPC service phụ chạy được và demo rõ.
- [ ] Có ít nhất 2 báo cáo/thống kê.
- [ ] Có Swagger/Postman collection.
- [ ] Có ERD, security matrix, endpoint list và hướng dẫn chạy.

## 4. Thứ tự ưu tiên khi thiếu thời gian

- [ ] Ưu tiên 1: Phase 00-104 - luồng nghiệp vụ lõi chạy end-to-end.
- [ ] Ưu tiên 2: Phase 105-120 - report và yêu cầu kỹ thuật bắt buộc.
- [ ] Ưu tiên 3: Phase 129-136 - kiểm thử, tài liệu và gói nộp.
- [ ] Ưu tiên 4: Phase 121-128 - AI FAQ Assistant mở rộng.
- [ ] Chỉ làm tính năng khuyến khích như audit log, soft delete tổng quát, Docker hoặc Serilog sau khi các checkbox bắt buộc đã hoàn thành.
