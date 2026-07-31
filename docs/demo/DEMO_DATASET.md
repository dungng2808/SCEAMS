# SCEAMS – Phase 133 demo dataset

Development seed (`dotnet run --project SCEAMS.API -- --seed`) là idempotent và
chỉ thêm fixture có tiền tố `[DEMO]`. Không chạy seed bằng connection string
production.

## Tài khoản mẫu

| Role | Email | Password |
|---|---|---|
| Admin | `admin@sceams.edu.vn` | giá trị `SeedData:AdminPassword` local |
| Staff | `staff@sceams.edu.vn` | giá trị `SeedData:StaffPassword` local |
| Organizer | `organizer@sceams.edu.vn` | giá trị `SeedData:OrganizerPassword` local |
| Student | `student@sceams.edu.vn` | giá trị `SeedData:StudentPassword` local |

Mật khẩu không được commit. Người chấm đặt bốn giá trị bằng User Secrets hoặc
environment variables trước khi seed; trang `/System/DemoAccounts` chỉ hiển
thị email/role trong Development.

## Fixture trạng thái

- Club: `[DEMO] Pending Club`, `[DEMO] Rejected Club`, `[DEMO] Dissolved Club`.
- Event: `[DEMO] Draft Event`, `Pending Approval`, `Completed`, `Cancelled`,
  `Rejected`, `Full Capacity`, `Deadline Passed` và `Venue Conflict`.
- Membership: `Pending`, `Active` (club chính), `Rejected`, `Removed`.
- Registration: `Pending`, `Confirmed`, `Attended`, `CancelledByStudent`.
- Attendance và feedback hợp lệ trên `[DEMO] Completed Event`.
- `[DEMO] Full Capacity Event` có capacity 1 và đã có registration.
- `[DEMO] Deadline Passed Event` có deadline trong quá khứ.
- `[DEMO] Venue Conflict Event` dùng cùng Venue/thời gian overlap với Event
  Approved để kiểm tra approve trả `409`.

Seed không thay đổi dữ liệu đã tồn tại: mỗi fixture được tìm theo tên trước khi
tạo. Có thể chạy lại để kiểm tra tính idempotent.

## Lệnh tạo dữ liệu

```bash
ASPNETCORE_ENVIRONMENT=Development \
SeedData__AdminPassword='<local-admin-password>' \
SeedData__StaffPassword='<local-staff-password>' \
SeedData__OrganizerPassword='<local-organizer-password>' \
SeedData__StudentPassword='<local-student-password>' \
dotnet run --project SCEAMS.API/SCEAMS.API.csproj -- --seed
```
