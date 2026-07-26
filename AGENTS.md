# AGENTS.md - Quy tắc triển khai SCEAMS

## Mục đích

File này là hướng dẫn bắt buộc cho mọi lần tiếp tục phát triển SCEAMS. Đọc file
này trước khi sửa code, sau đó đọc `SCEAMS_IMPLEMENTATION_ROADMAP.md` để biết
checkbox và phạm vi của phase đang làm. Không tự ý bỏ qua một checkbox của
roadmap hoặc gộp nhiều chức năng lớn vào cùng một phase.

SCEAMS là hệ thống quản lý câu lạc bộ và hoạt động ngoại khóa cho PRN232:

- Backend: ASP.NET Core Web API trên .NET 8.
- Client: ASP.NET Core MVC server-rendered gọi API thật qua `HttpClient`.
- Service phụ: ASP.NET Core gRPC (`SCEAMS.NotificationService`).
- Database: SQL Server với Entity Framework Core Code First.
- Authentication: JWT access token, refresh token rotation và role authorization.
- Múi giờ nghiệp vụ: `Asia/Ho_Chi_Minh`; lưu thời gian trong database bằng UTC.

## Tiến độ hiện tại

- Phase 00-36 đã hoàn thành theo roadmap.
- Phase tiếp theo là Phase 37: API danh sách CLB có OData.
- Không đánh dấu phase hoàn thành nếu chưa có kiểm thử thành công và Git commit
  riêng được push lên `origin`.

## Cấu trúc solution bắt buộc

Solution chỉ có đúng ba project chạy được:

```text
SCEAMS.sln
├── SCEAMS.API/
├── SCEAMS.MVC/
└── SCEAMS.NotificationService/
```

Không tạo thêm project class library cho Domain, Application hoặc Infrastructure.
Bốn layer của API là folder/namespace nằm trong cùng `SCEAMS.API.csproj`:

```text
SCEAMS.API/
├── Api/             # Controller, middleware và HTTP concerns
├── Application/    # DTO, Result, interface, validator, service nghiệp vụ
├── Domain/         # Entity, enum và hằng số nghiệp vụ thuần
└── Infrastructure/ # EF Core, repository, UoW, JWT, gRPC client, AI provider
```

## Quy tắc Clean Architecture

- `Domain` không tham chiếu EF Core, MVC, gRPC hoặc layer khác.
- `Application` chỉ phụ thuộc abstraction: DTO, `Result<T>`, interface
  repository/service/UoW, mapper, validator và business service.
- `Infrastructure` triển khai persistence và external service; repository chỉ
  truy vấn/lưu dữ liệu, không đặt business rule vào repository.
- Mọi thay đổi nhiều bảng hoặc có concurrency phải đi qua `IUnitOfWork` và
  transaction.
- Controller kế thừa `ApiControllerBase`, chỉ nhận request, gọi Application
  Service và chuyển `Result` thành HTTP response.
- Controller không được gọi trực tiếp `DbContext`, repository, UoW hoặc gRPC
  client.
- `Program.cs` là composition root, đăng ký DbContext, repository, UoW, service,
  JWT, OData, XML formatter và gRPC client.
- Luồng một chức năng API phải đi theo thứ tự:

```text
Domain -> Application DTO/interface -> Infrastructure implementation
-> Application service -> Api controller -> DI -> Swagger/Postman
-> MVC ApiClient -> MVC controller/view
```

## Cách chia và thực hiện phase

Mỗi chức năng phải được chia thành hai phase nhỏ liên tiếp:

1. Phase API: entity/DTO, validation, authorization, service, endpoint và
   request kiểm thử.
2. Phase MVC ngay sau đó: typed API client, controller, ViewModel, view, trạng
   thái loading/empty/success/error và kiểm thử gọi API thật.

Không truy cập `DbContext` hoặc database API trực tiếp từ MVC. Không dùng dữ
liệu giả để thay cho API thật.

Thứ tự nghiệp vụ trong roadmap:

- Phase 37-50: danh sách, chi tiết và workflow duyệt/từ chối/cập nhật/giải thể
  CLB.
- Phase 51-58: membership, xin gia nhập, duyệt/từ chối và loại thành viên.
- Phase 59-70: địa điểm, bảo trì, xóa và lịch sử sử dụng venue.
- Phase 71-90: Event, OData, tạo/sửa/gửi duyệt/duyệt/từ chối/hủy và đồng bộ trạng
  thái theo thời gian.
- Phase 91-104: đăng ký, hủy đăng ký, điểm danh và feedback.
- Phase 105-112: báo cáo và thống kê.
- Phase 113-120: JSON/XML content negotiation, gRPC notification và xử lý lỗi
  thống nhất.
- Phase 121-128: AI FAQ mở rộng, chỉ bắt đầu sau khi chức năng bắt buộc và gRPC
  ổn định.
- Phase 129-136: security audit, regression, E2E, dữ liệu demo, tài liệu,
  hướng dẫn chạy và gói nộp.

Chi tiết checkbox và endpoint của từng phase nằm trong
`SCEAMS_IMPLEMENTATION_ROADMAP.md`; phải cập nhật checkbox ở đó cùng lúc với
việc hoàn thành code.

## Quy ước API, dữ liệu và bảo mật

- Role hợp lệ: `Admin`, `Staff`, `Organizer`, `Student`.
- Status phải dùng enum/domain constants, không rải string tùy ý.
- Public chỉ thấy dữ liệu đã được duyệt/công khai; scope của Organizer luôn bị
  giới hạn theo Club/Event mà người đó phụ trách.
- Lấy user hiện tại từ JWT, không tin `UserId`, `StudentId` hoặc owner ID do
  client gửi lên.
- DTO response không chứa `PasswordHash`, refresh token thô hoặc thông tin nhạy
  cảm không cần thiết.
- Mật khẩu luôn hash; refresh token chỉ lưu dạng hash và phải rotation/revoke.
- Không cho mass assignment các field như role, status, owner, password hash.
- Status code phải nhất quán: `2xx` thành công, `400` validation,
  `401` thiếu/hết hạn xác thực, `403` sai role/ownership, `404` không tồn tại,
  `409` conflict nghiệp vụ, `429` rate limit và `500` lỗi ngoài dự kiến.
- Lỗi API phải dùng `ProblemDetails`, không trả stack trace, connection string
  hoặc secret trong production.
- Các workflow chính phải bảo toàn lịch sử; ưu tiên đổi status/soft delete thay
  vì hard-delete khi dữ liệu đã được tham chiếu.
- OData phải giới hạn `$top`, encode query an toàn và không ghép input chưa kiểm
  tra vào câu truy vấn.
- Nghiệp vụ có capacity, overlap lịch hoặc nhiều request đồng thời phải có
  transaction/concurrency control.

## Quy ước MVC và UI/UX

- MVC chỉ gọi API qua typed `HttpClient`/API client; mọi request có xác thực phải
  tự gắn `Authorization: Bearer <access-token>` qua handler dùng chung.
- Access/refresh token được lưu server-side Session hoặc encrypted HttpOnly
  authentication ticket, không lưu localStorage và không đưa ra JavaScript.
- Khi API trả `401` do access token hết hạn, thử refresh đúng một lần rồi retry
  request; refresh thất bại thì xóa session và chuyển về login với return URL.
- Mọi form phải có anti-forgery token, server-side validation và hiển thị lỗi
  theo field khi API trả validation/conflict.
- Mọi list/detail/form phải có trạng thái loading, empty, success, error và xử
  lý rõ `400/401/403/404/409/500`.
- Action/menu phải kiểm tra role và ownership ở cả UI lẫn API; UI không phải
  lớp bảo mật duy nhất.
- Khi sửa UI, giữ thiết kế nhất quán, responsive, accessible, focus rõ ràng,
  thông báo lỗi dễ hiểu và không xóa dữ liệu trên UI trước khi API xác nhận.
- Với thay đổi giao diện đáng kể, sử dụng skill `ui-ux-pro-max` theo hướng dẫn
  của skill và kiểm tra lại trên viewport desktop/mobile.

## Cấu hình và secret

- Không xóa hoặc thay thế `ConnectionStrings:DefaultConnection` của người dùng.
- Không commit connection string thật, JWT signing key, mật khẩu seed, AI key,
  refresh token hoặc dữ liệu database cá nhân.
- Ưu tiên `SCEAMS.API/appsettings.Local.json` (đã ignore), .NET User Secrets
  hoặc environment variables như `ConnectionStrings__DefaultConnection` và
  `Jwt__SigningKey`.
- `appsettings.Local.example.json` là template để người khác copy thành file
  local; file local không được đưa vào commit.
- Giữ nguyên các thay đổi local không thuộc phase, đặc biệt
  `SCEAMS.API/appsettings.json` và thư mục `.codex/`; không stage chúng nếu
  người dùng chưa yêu cầu.

## Definition of Done cho một phase

Trước khi đánh dấu `[x]`, phải hoàn thành tất cả mục sau:

- Code nằm đúng project/folder layer và không tạo project ngoài ba project chuẩn.
- API build được, endpoint chạy đúng bằng Swagger hoặc Postman.
- MVC gọi API thật và kiểm tra được luồng thành công cùng ít nhất một luồng lỗi.
- Đã kiểm tra authentication, role và ownership phù hợp.
- Đã kiểm tra status code, validation, empty state và lỗi kết nối API.
- Không còn exception chưa xử lý, secret hoặc dữ liệu test rác trong repository.
- Cập nhật README/roadmap/Postman/docs cần thiết cho chức năng mới.
- Chạy build/test và kiểm tra diff trước commit.

Các lệnh tối thiểu:

```bash
dotnet restore SCEAMS.sln
dotnet build SCEAMS.sln
dotnet test SCEAMS.sln --no-build --verbosity minimal
git diff --check -- ':!SCEAMS.API/appsettings.json'
```

Nếu có database test hoặc server chạy thật, phải dọn toàn bộ user/category/event
tạm sau kiểm thử và xác nhận không còn bản ghi test.

## Quy trình Git bắt buộc

Sau khi một phase đạt Definition of Done:

1. Kiểm tra `git status` và chỉ stage file thuộc phase.
2. Tuyệt đối không stage `SCEAMS.API/appsettings.json`, `.codex/` hoặc thay đổi
   không liên quan của người dùng.
3. Tạo một commit riêng bằng tiếng Việt theo mẫu:

   ```text
   phase NN: <mô tả ngắn bằng tiếng Việt>
   ```

4. Push ngay commit đó lên `origin` của branch hiện tại.
5. Xác nhận `HEAD` và `origin/<branch>` cùng commit trước khi bắt đầu phase mới.
6. Nếu build/test hoặc push thất bại, không đánh dấu phase hoàn thành; sửa hoặc
   báo rõ blocker.

Không dùng `git reset --hard`, `git checkout --` hoặc lệnh xóa diện rộng. Bảo
tồn mọi thay đổi có sẵn của người dùng.

## Checklist khi bắt đầu phase tiếp theo

- [ ] Đọc phần phase tương ứng trong `SCEAMS_IMPLEMENTATION_ROADMAP.md`.
- [ ] Kiểm tra phase trước đã có commit/push và working tree không bị trộn file.
- [ ] Xác định endpoint, role, DTO, status code, dữ liệu liên quan và test case.
- [ ] Nếu là API, hoàn thành service/validation/authorization trước MVC.
- [ ] Nếu là MVC, dùng API client thật, bổ sung ViewModel/controller/view và UI
      states.
- [ ] Cập nhật Swagger/Postman, README hoặc docs nếu endpoint/luồng thay đổi.
- [ ] Build, test, kiểm tra diff và dọn dữ liệu test.
- [ ] Tick checkbox roadmap, commit tiếng Việt và push.

