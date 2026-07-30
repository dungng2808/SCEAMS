# SCEAMS – Phase 129 Security Audit

Ngày rà soát: 31/07/2026
Phạm vi: `SCEAMS.API`, `SCEAMS.MVC`, `SCEAMS.NotificationService` và các request
trong `postman/SCEAMS.postman_collection.json`.

## Kết luận

Security audit được thực hiện theo bốn role nghiệp vụ (`Admin`, `Staff`,
`Organizer`, `Student`) và anonymous. Bộ kiểm thử tự động ở
`postman/SCEAMS.security-audit.postman_collection.json` kiểm tra các bề mặt
authorization, ownership và dữ liệu nhạy cảm. Collection không chứa mật khẩu,
JWT, refresh token hoặc connection string; các token phải được truyền bằng
environment variable local.

## Security matrix

| Nhóm tài nguyên | Anonymous | Admin | Staff | Organizer | Student |
|---|---:|---:|---:|---:|---:|
| Health và danh sách công khai (club/category/venue/event) | 200 | 200 | 200 | 200 | 200 |
| Đăng ký, login, refresh, revoke | 2xx/4xx | 2xx/4xx | 2xx/4xx | 2xx/4xx | 2xx/4xx |
| User list và CRUD user | 401 | 200 | 403 | 403 | 403 |
| Hồ sơ `/api/users/me` | 401 | 200 | 200 | 200 | 200 |
| CRUD category | 401 | 2xx | 403 | 403 | 403 |
| Tạo/cập nhật Club | 401 | 2xx | 403 | 2xx trong ownership | 403 |
| Approve/reject/dissolve Club | 401 | 2xx | 2xx | 403 | 403 |
| Membership request | 401 | 403 | 403 | 403 | 2xx |
| Duyệt/quản lý membership | 401 | 2xx | 2xx | 2xx trong ownership | 403 |
| Tạo/submit Event | 401 | 403 | 403 | 2xx trong ownership | 403 |
| Approve/reject Event và sync trạng thái | 401 | 2xx | 2xx (không sync) | 403 | 403 |
| Đăng ký/hủy Event và feedback | 401 | 403 | 403 | 403 | 2xx |
| Check-in | 401 | 403 | 403 | 2xx trong Event ownership | 403 |
| Reports | 401 | 200 | 200 | 200 theo club scope | 403 |
| Notification logs và reminders | 401 | 200 | 200 | 403 | 403 |
| Chatbot retrieval/ask/history | 401 | 403 | 403 | 403 | 2xx, chỉ dữ liệu của mình |

`2xx trong ownership` không phải quyền toàn cục: Application Service luôn lấy
user hiện tại từ JWT và đối chiếu `CreatedByUserId`, `ClubId` hoặc `EventId`.
Client không thể nâng quyền bằng cách gửi thêm `Role`, `OwnerId`, `Status` hoặc
`StudentId` trong request.

## Các kiểm tra đã thực hiện

### Authentication và authorization

- JWT validate issuer, audience, signing key, lifetime và role claim `role`.
- Endpoint yêu cầu đăng nhập trả `401` dưới dạng `application/problem+json` khi
  không có token; token đúng role được phép; token sai role trả `403`.
- Refresh token được rotation, chỉ lưu SHA-256 hash và token cũ bị từ chối.
- MVC giữ token ở server-side Session/encrypted HttpOnly ticket, không dùng
  localStorage và không đưa token ra JavaScript.

### Mass assignment và IDOR

- Request DTO không có `PasswordHash`, refresh token hash, role/status/owner
  của các workflow. Role của đăng ký luôn là `Student`.
- Service lấy subject từ claim `sub`/`ClaimTypes.NameIdentifier`, không tin
  `UserId`, `StudentId` hoặc owner ID do client gửi.
- Organizer bị giới hạn theo Club/Event phụ trách; Student chỉ đọc history và
  tạo registration/feedback của chính mình.
- Response DTO được kiểm tra không chứa `PasswordHash`; refresh token chỉ xuất
  hiện ở contract login/refresh và không được log hoặc trả ở resource khác.

### Transport, CORS, secret và logging

- API và MVC dùng `UseHttpsRedirection`; production bật HSTS. API không bật
  CORS mở rộng, vì vậy trình duyệt chặn origin ngoài danh sách (mặc định deny).
- `Jwt:SigningKey`, connection string, seed password và `AI:ApiKey` phải đến từ
  User Secrets hoặc environment variables. `appsettings.Local.json` được ignore;
  file `appsettings.Local.example.json` chỉ chứa placeholder.
- Exception middleware ghi log server-side kèm trace ID nhưng response chỉ có
  ProblemDetails tổng quát, không trả stack trace, SQL, connection string hay
  secret.
- Không commit dữ liệu database, token, password hoặc log vào repository.

## Chạy kiểm thử

1. Cấu hình API và các token local (không commit environment file):

   ```bash
   cp postman/SCEAMS.security-audit.environment.example.json /tmp/sceams-security.json
   # điền baseUrl và token của bốn role trong file local
   ```

2. Chạy static gate:

   ```bash
   bash tools/security-audit.sh --source-only
   ```

3. Khi đã có API và dữ liệu demo đang chạy, chạy Newman:

   ```bash
   newman run postman/SCEAMS.security-audit.postman_collection.json \
     -e /tmp/sceams-security.json --bail
   ```

Collection dùng các biến `adminAccessToken`, `staffAccessToken`,
`organizerAccessToken`, `studentAccessToken`, `otherClubId` và `otherEventId`.
Các ID ownership phải trỏ tới tài nguyên thuộc role khác để chứng minh `403`;
không dùng ID mặc định trong source.

## Bằng chứng và tiêu chí đạt

- `curl -k -i https://localhost:7195/api/health` trả `200`.
- `curl -k -i https://localhost:7195/api/users` không có token trả `401`
  `application/problem+json`.
- Newman đã chạy collection với API development đang bật: **14 request, 18
  assertion, 0 failure**. Lần chạy qua HTTPS local dùng cờ `--insecure` chỉ để
  bỏ qua certificate development tự ký; production phải dùng certificate được
  tin cậy.
- `dotnet build SCEAMS.sln --no-restore` và `dotnet test SCEAMS.sln --no-build`
  phải thành công.
- Không có thay đổi nào được stage từ `appsettings.json`, `.codex/` hoặc file
  local của người dùng.
