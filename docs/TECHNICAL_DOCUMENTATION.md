# SCEAMS – Tài liệu kỹ thuật (Phase 134)

## 1. Giới thiệu, mục tiêu và phạm vi

SCEAMS (Student Club & Extracurricular Activity Management System) quản lý
Club, Venue, Event, Registration, Attendance, Feedback, báo cáo và notification
cho hoạt động ngoại khóa. Backend là ASP.NET Core Web API .NET 8 + SQL Server /
EF Core Code First; client là ASP.NET Core MVC server-rendered; notification là
ASP.NET Core gRPC. API dùng JWT access token, refresh-token rotation, RBAC và
ProblemDetails. Múi giờ nghiệp vụ `Asia/Ho_Chi_Minh`, database lưu UTC.

## 2. Actor và use case

| Actor | Use case chính |
|---|---|
| Admin | Quản lý user/role, category, venue; approve/reject/dissolve Club; duyệt Event; reports, reminder và notification log |
| Staff | Duyệt Club/Event, vận hành venue, reports, reminder và tra cứu notification |
| Organizer | Tạo Club/Event, submit/sửa/cancel trong ownership, xem registration và check-in |
| Student | Đăng ký/hủy Event, xin gia nhập Club, xem lịch sử, check-in status và feedback sau Attended |

Anonymous chỉ xem resource công khai đã được lọc; không phải role nghiệp vụ.

## 3. ERD, khóa và migration

ERD Mermaid đầy đủ PK, FK và cardinality nằm tại
[`docs/architecture/SCEAMS_ERD.mmd`](architecture/SCEAMS_ERD.mmd). Các quan hệ
nhiều-nhiều có thuộc tính được biểu diễn bằng `ClubMemberships` và
`Registrations`; `Attendance` là 1–1 với Registration; `Feedback` unique theo
Event + Student. Migration hiện có trong
`SCEAMS.API/Infrastructure/Data/Migrations/`.

```bash
dotnet ef migrations list --project SCEAMS.API/SCEAMS.API.csproj --startup-project SCEAMS.API/SCEAMS.API.csproj
dotnet ef migrations script --project SCEAMS.API/SCEAMS.API.csproj --startup-project SCEAMS.API/SCEAMS.API.csproj --output docs/sceams.sql
```

## 4. Business rules và workflow

BR1–BR11 được kiểm tra trong [`docs/testing/BUSINESS_RULE_REGRESSION.md`](testing/BUSINESS_RULE_REGRESSION.md). Các invariant quan trọng: chỉ Event Approved trước deadline được đăng ký; transaction Serializable + unique index chống overbooking/trùng; chỉ Attended feedback; Organizer phải sở hữu Club/Event; approve kiểm tra Venue maintenance/overlap; Completed/Cancelled không sửa core data; chatbot chỉ grounded trên retrieval và giới hạn 10 câu hỏi/giờ/Student.

Workflow chính:

```text
Club: PendingApproval -> Approved | Rejected -> Dissolved
Event: Draft -> PendingApproval -> Approved -> Ongoing -> Completed
       Draft/PendingApproval -> Rejected; Approved/Ongoing -> Cancelled
Registration: Pending -> Confirmed -> Attended; Pending/Confirmed -> CancelledByStudent
Membership: Pending -> Active | Rejected -> Removed
```

## 5. Solution và Clean Architecture

Solution có đúng ba project chạy được:

```text
SCEAMS.sln
├── SCEAMS.API/
│   ├── Api/             # Controller, auth boundary, middleware, Swagger
│   ├── Application/    # DTO, Result, interface, validator, use-case service
│   ├── Domain/         # Entity, enum, invariant thuần
│   └── Infrastructure/ # EF/repository/UoW, JWT, seed, gRPC client, AI provider
├── SCEAMS.MVC/         # Typed HttpClient, server-side session, controllers/views
└── SCEAMS.NotificationService/ # gRPC proto/server, dedup và acknowledgement
```

`Domain` không tham chiếu framework; `Application` chỉ phụ thuộc abstraction;
repository không chứa business rule; controller không gọi DbContext; `Program.cs`
là composition root. Luồng chuẩn: Domain → Application → Infrastructure → API
controller → Swagger/Postman → MVC ApiClient → MVC view.

## 6. Endpoint list (contract rút gọn)

| Method | Route | DTO/response | Role | Status chính |
|---|---|---|---|---|
| POST | `/api/auth/register` | RegisterStudentRequest → RegisteredStudentResponse | Anonymous | 201/400/409 |
| POST | `/api/auth/login` | LoginRequest → LoginResponse | Anonymous | 200/400/401/403 |
| POST | `/api/auth/refresh`, `/api/auth/revoke` | RefreshTokenRequest → token/204 | Anonymous | 200/204/400/401 |
| GET/POST/PUT | `/api/users`, `/api/users/{id}*` | User DTOs | Admin | 200/201/204/400/403/404/409 |
| GET/PUT | `/api/users/me*` | Current profile/password DTO | Authenticated | 200/204/400/401 |
| GET | `/api/health*` | Health DTO | Anonymous | 200/503 |
| GET/POST/PUT/DELETE | `/api/club-categories[/{id}]` | Category DTOs | Public GET; Admin mutate | 200/201/204/400/403/404/409 |
| GET/POST/PUT | `/api/clubs[/{id}]` | Club DTOs | Public GET; Organizer/Admin mutate | 200/201/400/403/404 |
| PUT | `/api/clubs/{id}/approve|reject|dissolve` | decision DTO | Admin/Staff | 204/400/403/404/409 |
| POST/GET/PUT | `/api/clubs/{id}/members*` | Membership DTOs | Student request; Admin/Staff/Organizer manage | 200/201/204/400/403/404/409 |
| GET/POST/PUT/DELETE | `/api/venues[/{id}]*` | Venue DTOs | Public GET; Admin/Staff mutate | 200/201/204/400/403/404/409 |
| GET/POST/PUT | `/api/events[/{id}]` | Event DTOs + OData | Public GET; Organizer/Admin/Staff mutate | 200/201/400/403/404/409/406 |
| PUT | `/api/events/{id}/submit|approve|reject|cancel` | decision DTO | Organizer or Admin/Staff | 204/400/403/404/409 |
| GET | `/api/events/pending-approval`, `/{id}/registrations` | paged Event/Registration DTO | Admin/Staff; Admin/Organizer scope | 200/403/404 |
| POST/PUT/GET | `/api/registrations*` | Registration/Attendance DTOs | Student; Organizer check-in | 200/201/204/400/401/403/404/409 |
| POST/GET | `/api/events/{id}/feedback` | Feedback DTO/Summary | Student create; public read | 200/201/400/403/404/409 |
| GET | `/api/reports/*` | report DTOs | Admin/Staff/(Organizer scope) | 200/403 |
| GET/POST | `/api/notifications/logs`, `/api/reminders/run` | notification/reminder DTOs | Admin/Staff | 200/403 |
| POST/GET | `/api/chatbot/retrieval`, `/api/chatbot/ask`, `/api/chatbot/history` | FAQ DTOs | Student | 200/401/403/429 |

Lỗi dùng RFC ProblemDetails; `401` thiếu/sai token, `403` role/ownership,
`404` không tồn tại, `409` conflict nghiệp vụ, `406` Accept không hỗ trợ.

## 7. Security matrix

| Capability | Admin | Staff | Organizer | Student |
|---|---:|---:|---:|---:|
| User/role/category | ✓ | – | – | – |
| Venue | ✓ | ✓ | – | – |
| Club approve/reject/dissolve | ✓ | ✓ | – | – |
| Club/Event create and update | ✓ | – | ✓ (owner) | – |
| Event approve/reject | ✓ | ✓ | – | – |
| Registration/feedback | – | – | – | ✓ (owner/Attended) |
| Check-in/registration list | – | – | ✓ (owner) | – |
| Reports | ✓ | ✓ | ✓ (scope) | – |
| Notification logs/reminder | ✓ | ✓ | – | – |
| Chatbot FAQ/history | – | – | – | ✓ (own history) |

JWT issuer/audience/signing key/lifetime được validate; refresh token lưu hash và
rotation; password hash không có trong DTO; MVC token ở Session/HttpOnly ticket;
HTTPS/HSTS production, CORS mặc định deny, secret từ User Secrets/environment.
Chi tiết evidence ở [`docs/security/SECURITY_AUDIT.md`](security/SECURITY_AUDIT.md).

## 8. OData, JSON/XML, gRPC và AI FAQ

- OData Events hỗ trợ `$filter`, `$orderby`, `$select`, `$expand`, `$count`,
  `$top ≤ 50`; request/response JSON/XML ở
  [`docs/api/ODATA_CONTENT_NEGOTIATION.md`](api/ODATA_CONTENT_NEGOTIATION.md).
- MVC/API dùng content negotiation JSON mặc định và XML serializer khi
  `Accept: application/xml`; `text/csv` trả `406`.
- API gọi `INotificationClientService` (Infrastructure gRPC client) →
  `NotificationGrpcService` qua `notification.proto`; correlation/event/type
  unique để dedup, acknowledgement trả về API. Sequence chi tiết ở
  [`docs/testing/E2E_ROLE_SMOKE.md`](testing/E2E_ROLE_SMOKE.md).
- AI FAQ: retrieval chỉ lấy Event Approved đã kiểm chứng, provider nằm sau
  `IAiProvider`, timeout/rate-limit/fallback trả lời không biết nếu không có
  context. Không gửi password/JWT/refresh token/PII không cần thiết; AI key chỉ
  ở User Secrets/environment, không log raw prompt; mutation vẫn qua RBAC API.
