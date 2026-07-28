# SCEAMS

Student Club & Extracurricular Activity Management System for PRN232.

## Technology baseline

- .NET SDK 8.0.423
- ASP.NET Core Web API
- ASP.NET Core MVC
- ASP.NET Core gRPC
- Entity Framework Core 8 with SQL Server
- JWT authentication

The repository is pinned to .NET 8 through `global.json`.

## Solution structure

The solution contains exactly three runnable projects:

```text
SCEAMS.sln
├── SCEAMS.API/
├── SCEAMS.MVC/
└── SCEAMS.NotificationService/
```

`SCEAMS.API` will follow the four-layer folder structure used by
`LibraryApi_4Layers`:

```text
SCEAMS.API/
├── Api/
├── Application/
├── Domain/
└── Infrastructure/
```

These layers remain folders and namespaces inside one API project. They are not
separate class-library projects.

## Time handling

- Business timezone: `Asia/Ho_Chi_Minh`
- Persist timestamps in UTC.
- Convert timestamps for display at the MVC boundary.

## Initial commands

```bash
dotnet --version
dotnet restore SCEAMS.sln
dotnet build SCEAMS.sln
dotnet sln SCEAMS.sln list
```

## SQL Server configuration

The API reads `ConnectionStrings:DefaultConnection` from configuration. In
Development, copy `SCEAMS.API/appsettings.Local.example.json` to
`SCEAMS.API/appsettings.Local.json` and replace the placeholder values. The
local file is ignored by Git and is loaded automatically by the API.

You can also use .NET User Secrets or the
`ConnectionStrings__DefaultConnection` environment variable. Environment
variables take precedence over the local JSON file.

Example User Secrets command:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=SCEAMS;User Id=sa;Password=<your-password>;TrustServerCertificate=True;Encrypt=False" \
  --project SCEAMS.API/SCEAMS.API.csproj
```

Apply the database migration:

```bash
ASPNETCORE_ENVIRONMENT=Development \
  dotnet ef database update \
  --project SCEAMS.API/SCEAMS.API.csproj \
  --startup-project SCEAMS.API/SCEAMS.API.csproj
```

## Development seed

The seed command requires four password values from User Secrets or environment
variables. Passwords are hashed before being stored and are never written to a
migration or application log.

```bash
ASPNETCORE_ENVIRONMENT=Development \
SeedData__AdminPassword="<admin-password>" \
SeedData__StaffPassword="<staff-password>" \
SeedData__OrganizerPassword="<organizer-password>" \
SeedData__StudentPassword="<student-password>" \
dotnet run --project SCEAMS.API/SCEAMS.API.csproj -- --seed
```

This single command migrates a new database and prepares all demo data. It is
idempotent and can be executed more than once.

After starting the API and MVC projects in Development, open:

- `http://localhost:5206/System/Health` to verify the demo seed status.
- `http://localhost:5206/System/DemoAccounts` to view the demo emails by role.

The demo-account page is unavailable outside Development and never displays
passwords.

## Student registration API

Create a Student account with:

```text
POST http://localhost:5195/api/auth/register
```

The request accepts `fullName`, `email`, `studentCode`, optional `phoneNumber`,
`password` and `confirmPassword`. Passwords must have at least eight characters
and include uppercase, lowercase, numeric and special characters.

The API normalizes email and student code, rejects duplicate values, hashes the
password and always assigns the `Student` role. A successful `201 Created`
response never contains the password or password hash. A ready-to-run request
template is available in `SCEAMS.API/SCEAMS.API.http`; define
`RegisterPassword` only in your local HTTP-client environment.

## MVC Student registration

With the API and MVC projects running, open:

```text
http://localhost:5206/Account/Register
```

The MVC form applies client-side and server-side validation, maps API conflicts
back to the corresponding email or student-code field, and redirects successful
registrations to `/Account/Login`. The login page is currently a handoff page;
the JWT API is available from Phase 09 and the MVC token flow is implemented in
Phase 10.

## JWT login API

The API requires a signing key with at least 32 characters. Keep it outside the
repository:

```bash
dotnet user-secrets set \
  "Jwt:SigningKey" \
  "<a-random-secret-with-at-least-32-characters>" \
  --project SCEAMS.API/SCEAMS.API.csproj
```

Login with:

```text
POST http://localhost:5195/api/auth/login
```

The request accepts `email` and `password`. A successful response contains a
Bearer access token, a refresh token, their UTC expiries and safe user
information. The JWT is signed with HMAC SHA-256 and contains `sub`, `email`,
`role` and `jti` claims. Invalid credentials return `401`; an inactive account
returns `403`.

Refresh an expired access token with:

```text
POST http://localhost:5195/api/auth/refresh
```

Send only the current `refreshToken` in the JSON body. Each successful refresh
rotates both tokens. Only the SHA-256 hash of the refresh token is stored in
SQL Server; the previous token, an expired token or a revoked token returns
`401`.

Use the Swagger **Authorize** action or the Phase 09 requests in
`postman/SCEAMS.postman_collection.json`. Password variables and captured
access/refresh tokens are marked secret and have no committed values.

## MVC login and server-side token session

Open the MVC login page:

```text
http://localhost:5206/Account/Login
```

After a successful API login, MVC stores access and refresh tokens in
server-side Session and an encrypted, HttpOnly ASP.NET authentication ticket.
The tokens are never exposed to JavaScript or localStorage.

`BearerTokenHandler` automatically adds `Authorization: Bearer <token>` to
typed API-client requests. If an API request returns `401`, the handler rotates
the refresh token and retries the original request exactly once, including its
body. Concurrent expired requests share one refresh operation. If refresh
fails, MVC clears authentication and redirects to login with the original
return URL.

Logout calls `POST /api/auth/revoke` before clearing Session and the encrypted
authentication cookie. Local logout still completes if the API is temporarily
unavailable.

Each demo role has a separate authenticated landing URL:

- `/Dashboard/Admin`
- `/Dashboard/Staff`
- `/Dashboard/Organizer`
- `/Dashboard/Student`

The current development Session store is in memory. If it is lost during a
process restart, the encrypted authentication ticket provides the token
fallback while it remains valid. A distributed cache can replace the in-memory
store later without changing the controller or handler flow.

## Admin user-list API

Only an authenticated `Admin` can retrieve the user list:

```text
GET http://localhost:5195/api/users
Authorization: Bearer <admin-access-token>
```

The endpoint accepts optional `search`, `role` and `isActive` filters plus
`page` and `pageSize`. Search covers full name, email, student code and phone
number. Valid roles are `Admin`, `Staff`, `Organizer` and `Student`; page size
is limited to 100.

The response contains `items`, `page`, `pageSize`, `totalItems`, `totalPages`,
`hasPreviousPage` and `hasNextPage`. Each item contains only safe profile data;
password and refresh-token hashes are neither queried nor returned. A valid
Staff, Organizer or Student token receives `403 Forbidden`.

## MVC Admin user list

After signing in as Admin, open:

```text
https://localhost:7034/Admin/Users
```

The page provides full-text search, role and active-status filters, selectable
page size and numbered pagination. Pagination links preserve all active filter
values in the query string. The responsive table displays only safe account
fields and converts creation timestamps to the SCEAMS business timezone.

No-result filters render a dedicated empty state. Non-Admin MVC users are sent
to a styled `403 Forbidden` page, while an API-side `403` is also handled
inside the Admin Users screen.

## Admin create-user API

Only an authenticated Admin can create an account:

```text
POST http://localhost:5195/api/users
Authorization: Bearer <admin-access-token>
```

The request accepts full name, email, optional student code and phone number,
role, active status, initial password and password confirmation. Role is one of
`Admin`, `Staff`, `Organizer` or `Student`; a Student account requires a student
code. Passwords follow the same complexity policy as Student registration.

Email and student code are normalized and checked for uniqueness. The initial
password is hashed before persistence and is never returned. Successful
creation returns `201 Created` with safe account fields; a duplicate email or
student code returns `409 Conflict`. Ready-to-run requests are available in
`SCEAMS.API/SCEAMS.API.http` and the Postman **Users** folder.

## MVC Admin create user

After signing in as Admin, select **Tạo tài khoản** from the user list or open:

```text
https://localhost:7034/Admin/Users/Create
```

The form exposes only the four API-supported roles and applies the same name,
email, student-code, phone and password rules before submitting. Student code
is required when the selected role is `Student`; the Admin can also choose
whether the new account starts active.

API validation and conflicts are returned to the corresponding form field. On
successful creation, MVC redirects to `/Admin/Users` with the new email as the
search filter, reloads the list from the API and displays a success
notification so the Admin can confirm the persisted account is present.

## Admin update-user API

Only an authenticated Admin can update another account's profile:

```text
PUT http://localhost:5195/api/users/{id}
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

Example request:

```json
{
  "fullName": "Updated Staff Account",
  "email": "updated.staff@sceams.edu.vn",
  "studentCode": null,
  "phoneNumber": "0912345678"
}
```

This endpoint updates profile fields only. Its request DTO does not expose
`role`, `isActive`, password or token fields, so role and account status remain
unchanged. Email and student code are normalized and must remain unique; a
Student account must retain a student code.

Success returns `200 OK` with safe persisted account fields. Invalid profile
data returns `400`, an unknown user ID returns `404`, and an email or student
code already assigned to another account returns `409`. Ready-to-run Phase 23
requests are available in `SCEAMS.API/SCEAMS.API.http` and the Postman
**Users** folder.

## MVC Admin edit user

After signing in as Admin, select **Sửa** on `/Admin/Users` or open:

```text
https://localhost:7034/Admin/Users/{id}/Edit
```

MVC loads the exact account by ID through the Admin user-list API and pre-fills
only full name, email, optional student code and phone number. Role and active
status are displayed as protected context; they are not rendered as form
inputs and are not included in the update request.

MVC validation and API conflicts are displayed next to the corresponding
field. After a successful `PUT /api/users/{id}`, MVC retrieves the account from
the API again, then redirects to the user list filtered by the confirmed email
and displays a success notification. A missing account renders a dedicated
`404` state without an editable form.

## Admin account active-status API

Only an authenticated Admin can lock or unlock an account:

```text
PUT http://localhost:5195/api/users/{id}/active-status
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

Use `{ "isActive": false }` to lock and `{ "isActive": true }` to unlock.
Success returns `200 OK` with safe account identity, role and the persisted
active status. Repeating the same desired state is idempotent.

An Admin cannot lock the account represented by the current JWT; that request
returns `400`. An unknown target returns `404`, and non-Admin roles receive
`403`. Locking also clears the stored refresh-token hash and expiry. The locked
account receives `403` on login and any refresh token issued before locking
receives `401`; an access token already issued remains subject to its normal
short expiry.

Ready-to-run Phase 25 requests are available in
`SCEAMS.API/SCEAMS.API.http` and the Postman **Users** folder.

## Admin user-role API

Only an authenticated Admin can assign a role to an account:

```text
PUT http://localhost:5195/api/users/{id}/role
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

The request accepts exactly one of the four domain roles:

```json
{
  "role": "Organizer"
}
```

Supported values are `Admin`, `Staff`, `Organizer` and `Student`. A successful
request returns `200 OK` with safe account identity, the persisted role and
active status. Invalid or missing role values return `400`, an unknown account
returns `404`, and authenticated non-Admin users receive `403`.

When the role actually changes, the API clears the target account's stored
refresh-token hash and expiry in the same save. A refresh token issued before
the change therefore returns `401`; the account must sign in again to receive
claims for its new role. Repeating the current role is idempotent and does not
revoke an otherwise valid refresh token.

The final active Admin cannot demote their own account. This protection counts
only active Admin accounts so an inactive Admin cannot leave the system without
an account capable of restoring administrative access.

Ready-to-run Phase 27 requests and failure cases are available in
`SCEAMS.API/SCEAMS.API.http` and the Postman **Users** folder.

## MVC user-role assignment

The Admin user list exposes role assignment as a separate **Vai trò** action;
it is not mixed into the profile-edit form. The action opens a confirmation
dialog with the target account, current role, new-role selector and a security
notice explaining that the current refresh token will be revoked. The submit
button remains disabled until a different valid role is selected.

MVC posts the anti-forgery-protected form to:

```text
POST /Admin/Users/{id}/Role
```

The controller validates the selected role, loads the current account, calls
`PUT /api/users/{id}/role`, and then loads the account from the API again. Only
after that confirmation does the list display the persisted role badge and the
**Phiên cũ đã thu hồi** security badge. The result redirects to a search for the
updated account so a previous role filter cannot hide it.

The previous refresh token returns `401` after a real role change. The account
must sign in again and receives a new access token whose role claim matches the
persisted role. Submitting the current role is rejected by MVC without revoking
the existing session. API authorization and last-active-Admin errors are
displayed on the user list without losing the active filters.

## Club-category list API

Public visitors and authenticated users can retrieve the shared club-category
list:

```text
GET http://localhost:5195/api/club-categories
Accept: application/json
```

No access token is required. Sending a valid token for any role returns the same
list. Each item is a compact DTO containing only `id`, `name` and
`description`:

```json
[
  {
    "id": 1,
    "name": "Academic and Technology",
    "description": "Clubs focused on academic and technology activities."
  }
]
```

The repository projects these fields directly in SQL without loading the
`Clubs` navigation. Results are ordered by name and then ID for deterministic
output. When no categories exist, the endpoint returns `200 OK` with an empty
JSON array.

Ready-to-run public and authenticated Phase 29 requests are available in
`SCEAMS.API/SCEAMS.API.http` and the Postman **Club Categories** folder.

## MVC club-category list

Open the shared club-category page at:

```text
https://localhost:7034/ClubCategories
```

The page is available to public visitors and every authenticated role. It loads
the Phase 29 API through a typed server-side client, displays the category
count, and renders responsive category cards without exposing access tokens to
the browser. A **Danh mục CLB** link is available in the main navigation and
footer.

Only an authenticated Admin sees the management toolbar and category-creation
action; other roles and public visitors do not receive the toolbar markup. An
empty API result renders a dedicated `Chưa có danh mục câu lạc bộ` state,
while connection failures show retry and system-health actions.

## Admin create club-category API

Only an authenticated Admin can create a category:

```text
POST http://localhost:5195/api/club-categories
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

```json
{
  "name": "Community Service",
  "description": "Clubs focused on volunteering and community projects."
}
```

The category name is required and limited to 150 characters. Leading,
trailing and repeated spaces are normalized before persistence. Description is
optional and limited to 1,000 characters. Names are checked
case-insensitively, so submitting `community service` after
`Community Service` returns `409 Conflict`.

A successful request returns `201 Created` with the compact `id`, `name` and
`description` DTO. Missing authentication returns `401`, authenticated
non-Admin roles receive `403`, and invalid input returns `400`. Ready-to-run
Phase 31 requests are available in `SCEAMS.API/SCEAMS.API.http` and the
Postman **Club Categories** folder.

## MVC create club category

After signing in as Admin, select **Thêm danh mục** on the shared category page
or open:

```text
https://localhost:7034/ClubCategories/Create
```

The responsive form validates the required name and the API limits for name
and description before submitting through the authenticated server-side
client. Its anti-forgery token protects the POST request. Public visitors are
redirected to Login and authenticated non-Admin roles are sent to the styled
access-denied page.

API validation errors stay on the form. In particular, a case-insensitive
duplicate name is attached to the `Name` field as
`Tên danh mục này đã tồn tại.` On success, MVC redirects to
`/ClubCategories`, reloads the categories from the public API, displays a
success notification and marks the newly created card with **Vừa tạo**.

## Admin update club-category API

Only an authenticated Admin can update a category:

```text
PUT http://localhost:5195/api/club-categories/{id}
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

```json
{
  "name": "Community Engagement",
  "description": "Clubs focused on volunteering and community projects."
}
```

The endpoint applies the same validation and whitespace normalization as
category creation. It returns `404 Not Found` when the category does not
exist, or `409 Conflict` when another category already uses the submitted name
regardless of casing. A successful request returns `200 OK` with the compact
`id`, `name` and `description` DTO.

The service retrieves the tracked category and changes only its `Name` and
`Description` scalar properties. It neither loads nor replaces the `Clubs`
navigation, so every existing Club keeps the same `CategoryId` relationship.
Ready-to-run Phase 33 requests are available in
`SCEAMS.API/SCEAMS.API.http` and the Postman **Club Categories** folder.

## MVC edit club category

Admins can select **Chỉnh sửa** on any category card or open:

```text
https://localhost:7034/ClubCategories/{id}/Edit
```

MVC first loads the current category from the shared API client and renders a
pre-filled, anti-forgery-protected form. Public visitors are redirected to
Login and authenticated non-Admin roles are sent to Access Denied. A missing
category renders a clear `404 · Không tìm thấy` state; API duplicate-name
conflicts stay on the form and are attached to the `Name` field.

After a successful update, MVC redirects to `/ClubCategories`, reloads the
categories from the API, shows a success notification and marks the updated
card with **Vừa cập nhật**. The edit form changes only the category's display
name and description; Club relationships remain untouched.

## MVC account lock and unlock

On the Admin user list, each account now has a separate lock or unlock action.
The action opens a confirmation dialog that shows the target account and
explains the effect before anything is submitted. MVC sends the confirmed
request to `PUT /api/users/{id}/active-status` through the authenticated
server-side API client; the browser never receives the access token.

The confirmation form is protected by an anti-forgery token and preserves the
current search, role, status and page filters. After a successful API response,
MVC reloads the list and displays both a success notification and the persisted
active-status badge. API errors remain on the same filtered list. In
particular, an Admin who attempts to lock their own signed-in account sees the
localized error `Bạn không thể khóa tài khoản Admin đang đăng nhập.` and their
status remains active.

## Current-user profile API

An authenticated user can retrieve only their own profile:

```text
GET http://localhost:5195/api/users/me
Authorization: Bearer <access-token>
```

The API derives the user ID exclusively from the JWT `sub` claim; it does not
accept a user ID from route, query string or request body. The response contains
the safe profile fields needed by MVC: ID, full name, email, optional student
code and phone number, role, active status and UTC creation time. Password and
refresh-token data are never returned.

Missing or invalid authentication returns `401`. A valid token whose user has
been deleted returns `404`. The Postman **Users** folder contains ready-to-run
requests; execute **Login - Success** first to populate `accessToken`.

## MVC current-user profile

After logging in, open:

```text
http://localhost:5206/Profile
```

The page always loads the current profile through `GET /api/users/me` using the
server-side JWT flow. It displays the user's name, email, optional student code
and phone number, role, account status, and creation time converted from UTC to
Vietnam time (UTC+7).

The page is read-only: it has no controls for changing the role or account
status. If the API reports that the token or user is no longer valid, MVC clears
the Session and authentication cookie before returning to Login. Temporary API
or network failures keep the user on a safe error state with retry and system
health actions.

## Current-user profile update API

An authenticated user can update only their display name and optional phone
number:

```text
PUT http://localhost:5195/api/users/me
Authorization: Bearer <access-token>
Content-Type: application/json
```

Example request:

```json
{
  "fullName": "Updated Student Name",
  "phoneNumber": "0901234567"
}
```

The request DTO does not expose email, student code, role, active status,
password hash or refresh-token fields. The API normalizes whitespace in the
name, converts a blank phone number to `null`, validates the phone format and
returns the updated safe profile. Invalid input returns `400`; an invalid token
returns `401`; a valid token whose user no longer exists returns `404`.

## MVC profile editing

After logging in, use the **Chỉnh sửa** action on `/Profile` or open:

```text
http://localhost:5206/Profile/Edit
```

MVC first calls `GET /api/users/me` to pre-fill the editable name and phone
fields. The form does not render email, student code, role, active status,
password or token fields. Client and server validation errors are displayed
next to the corresponding input.

On a successful `PUT /api/users/me`, MVC refreshes the display-name claim,
redirects to `/Profile`, and calls `GET /api/users/me` again. The profile page
therefore confirms the persisted API data and displays a success notification
instead of relying on the submitted form values.

## Current-user password change API

An authenticated user can change their own password with:

```text
PUT http://localhost:5195/api/users/me/password
Authorization: Bearer <access-token>
Content-Type: application/json
```

Example request:

```json
{
  "currentPassword": "<current-password>",
  "newPassword": "<new-strong-password>",
  "confirmPassword": "<new-strong-password>"
}
```

The new password must contain at least eight characters with uppercase,
lowercase, numeric and special characters. The API verifies the current
password against its hash, hashes the new password and clears both the stored
refresh-token hash and expiry in the same database save. Success returns
`204 No Content`, so no password data is included in the response. An incorrect
current password or invalid new-password policy returns `400`.

## MVC password change

After logging in, use the **Đổi mật khẩu** action on `/Profile` or open:

```text
http://localhost:5206/Profile/Password
```

The form collects the current password, new password and confirmation. It
applies the same password policy on the client and MVC server, then sends only
those three values to `PUT /api/users/me/password`. Submitted passwords are
never rendered back into the HTML when validation or API errors occur.

After a successful `204 No Content`, MVC clears the server-side Session that
contains the JWT, signs out the encrypted authentication cookie and redirects
to Login with a success message. A copied pre-change browser cookie cannot
reuse the cleared server-side token and is redirected to Login. The user must
authenticate again with the new password.

## Club membership decisions API

An Admin, Staff member or the Organizer who owns a Club can approve or reject
one pending membership application:

```text
PUT http://localhost:5195/api/clubs/{clubId}/members/{userId}/decision
Authorization: Bearer <access-token>
Content-Type: application/json
```

Approve an application with:

```json
{
  "approve": true
}
```

Reject it with an optional reason:

```json
{
  "approve": false,
  "rejectionReason": "Không phù hợp với tiêu chí thành viên hiện tại."
}
```

The API only finds the membership belonging to the supplied `userId` inside
the supplied Club, and only a `Pending` membership can be processed. Approval
changes the status to `Active`; rejection changes it to `Rejected` and records
the decision user/time. A missing Club or membership returns `404`, a user
without ownership/permission returns `403`, and an already processed
membership returns `409`. Phase 55 requests are in the Postman **Clubs**
folder; set `decisionClubId` and `decisionUserId` to a pending application
before running the approve or reject request.

## API loại thành viên khỏi Club

Admin, Staff hoặc Organizer sở hữu Club có thể loại một thành viên đang Active:

```text
PUT http://localhost:5195/api/clubs/{clubId}/members/{userId}/remove
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request bắt buộc có lý do:

```json
{
  "reason": "Vi phạm quy định hoạt động của câu lạc bộ."
}
```

API chuyển status sang `Removed`, lưu lý do và giữ nguyên row membership để
bảo toàn lịch sử. Chỉ membership `Active` mới được loại; xử lý lại một row đã
`Removed` hoặc trạng thái khác trả `409`. Request Phase 57 nằm trong Postman
**Clubs** folder.

## MVC xử lý đơn gia nhập

Tại trang quản lý thành viên:

```text
https://localhost:7034/Clubs/{clubId}/Members
```

Organizer sở hữu Club, Admin và Staff nhìn thấy nút **Duyệt** và **Từ chối**
cho từng đơn Pending. Mỗi action dùng form POST có anti-forgery token và hộp
xác nhận; sau khi API trả thành công, MVC redirect về cùng trang để tải lại
danh sách. Nếu người khác đã xử lý đơn trước đó, API trả `409` và MVC hiển thị
thông báo lỗi mà không tự xóa dòng trên giao diện.

Trang này có thêm tab **Thành viên đang hoạt động**. Organizer sở hữu Club,
Admin và Staff có thể mở hộp thoại **Loại thành viên**, nhập lý do bắt buộc và
xác nhận. MVC gọi API remove rồi tải lại tab Active; thành viên đã chuyển sang
`Removed` sẽ không còn nằm trong danh sách Active nhưng lịch sử vẫn tồn tại.

## API danh sách địa điểm

Danh sách địa điểm được cung cấp công khai qua:

```text
GET http://localhost:5195/api/venues?search=Hall&maintenance=false&page=1&pageSize=10
```

API hỗ trợ tìm theo tên/vị trí, lọc `maintenance=true|false` và phân trang với
`pageSize` tối đa 50. Response chỉ chứa `id`, `name`, `location`, `capacity` và
`isUnderMaintenance`; không trả navigation `Events`, tránh vòng lặp khi dùng
JSON/XML. Request Phase 59 nằm trong Postman **Venues** folder.

## MVC danh sách địa điểm

Mở trang:

```text
https://localhost:7034/Venues
```

MVC có bộ lọc tên/vị trí, tình trạng bảo trì và page size; danh sách hiển thị
badge `Sẵn sàng`/`Đang bảo trì` cùng sức chứa. Admin và Staff nhìn thấy khu vực
quản trị địa điểm; các nút tạo/sửa/bảo trì sẽ được mở ở các phase tiếp theo.

## API tạo địa điểm

Admin và Staff có thể tạo venue bằng:

```text
POST http://localhost:5195/api/venues
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "name": "Innovation Hall",
  "location": "Building A - Floor 2",
  "capacity": 120
}
```

Tên, vị trí và sức chứa được validate; venue mới luôn bắt đầu ở trạng thái
`isUnderMaintenance: false`. Cặp `name + location` không được trùng, kể cả
khác hoa thường; lỗi trùng trả `409 Conflict`. Request Phase 61 nằm trong
Postman **Venues** folder.

## MVC tạo địa điểm

Admin và Staff mở form tại:

```text
https://localhost:7034/Venues/Create
```

Form có validation phía client/server cho tên, vị trí và capacity; conflict
`409` được gắn vào trường tên. Khi tạo thành công, MVC redirect về danh sách
với filter tên venue mới để xác nhận dữ liệu đã được lưu qua API.

## API cập nhật địa điểm

Admin và Staff cập nhật thông tin venue bằng:

```text
PUT http://localhost:5195/api/venues/{id}
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "name": "Innovation Hall Updated",
  "location": "Building A - Floor 2",
  "capacity": 150
}
```

Endpoint này chỉ thay đổi tên, vị trí và sức chứa; trạng thái bảo trì được giữ
nguyên để xử lý riêng ở Phase 65. API trả `409 Conflict` nếu cặp tên/vị trí đã
tồn tại hoặc sức chứa mới nhỏ hơn số đăng ký `Confirmed`/`Attended` của các
Event `Approved` sắp tới. Request kiểm thử nằm trong Postman **Venues** folder.

## MVC sửa địa điểm

Admin và Staff mở nút **Sửa** tại danh sách địa điểm hoặc truy cập:

```text
https://localhost:7034/Venues/{id}/Edit
```

Form tải dữ liệu hiện tại từ API, chỉ cho sửa tên, vị trí và capacity. Khi API
trả `409 Conflict` (trùng tên/vị trí hoặc giảm capacity dưới số đăng ký hợp lệ),
MVC giữ nguyên dữ liệu trên form và hiển thị lý do để người dùng điều chỉnh.
Lưu thành công sẽ redirect về danh sách, lọc theo venue vừa cập nhật để xác nhận
dữ liệu mới.

## API xóa địa điểm

Chỉ Admin được gọi:

```text
DELETE http://localhost:5195/api/venues/{id}
Authorization: Bearer <access-token>
```

Venue chưa từng được Event tham chiếu sẽ được hard-delete và trả `204 No Content`.
Nếu venue đã xuất hiện trong bất kỳ Event nào, API không xóa dữ liệu và trả
`409 Conflict` với hướng dẫn chuyển venue sang trạng thái bảo trì. Request kiểm
thử hai trường hợp nằm trong Postman **Venues** folder.

## API lịch sử sử dụng địa điểm

Tra cứu lịch venue theo khoảng thời gian:

```text
GET http://localhost:5195/api/venues/{id}/schedule?from=2026-01-01T00:00:00Z&to=2027-01-01T00:00:00Z
```

API trả các Event có thời gian giao với khoảng `from` - `to`. Admin/Staff xem
mọi trạng thái; request công khai hoặc role khác chỉ nhận Event `Approved` và
`Ongoing`. Khoảng không hợp lệ (`to <= from`) trả `400`; venue không tồn tại
trả `404`. Request mẫu nằm trong Postman **Venues** folder.

## MVC xóa địa điểm

Chỉ Admin thấy nút **Xóa** trong bảng venue. MVC dùng form POST có anti-forgery
token và hộp xác nhận; sau mọi kết quả đều redirect tải lại danh sách. Nếu API
trả `409` vì venue đã được Event tham chiếu, giao diện hiển thị hướng dẫn bật
maintenance và không loại bỏ dòng venue khỏi danh sách.

## API bật/tắt bảo trì địa điểm

Admin và Staff dùng endpoint riêng để thay đổi trạng thái bảo trì:

```text
PUT http://localhost:5195/api/venues/{id}/maintenance
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "isUnderMaintenance": true
}
```

Khi bật bảo trì, API kiểm tra các Event `Approved`/`Ongoing` chưa kết thúc đang
dùng venue. Nếu có xung đột, API trả `409 Conflict` với mảng `conflicts` gồm
`eventId`, `title`, `status`, `startTime` và `endTime`; trạng thái venue không
bị thay đổi. Tắt bảo trì không có điều kiện xung đột. Request kiểm thử nằm trong
Postman **Venues** folder.

## MVC lịch sử sử dụng địa điểm

Từ danh sách venue, chọn **Lịch** để mở:

```text
https://localhost:7034/Venues/{id}/Schedule
```

Trang cho phép chọn ngày bắt đầu/kết thúc, hiển thị bảng Event giao với khoảng
đã chọn và có trạng thái rỗng khi venue không có lịch. Các mốc thời gian UTC từ
API được hiển thị theo giờ địa phương của máy chạy MVC.

## MVC cập nhật bảo trì địa điểm

Trong danh sách địa điểm, Admin và Staff có thể dùng nút **Bảo trì** hoặc
**Tắt bảo trì**. MVC gửi form POST có anti-forgery token và hộp xác nhận trước
khi gọi API. Sau khi thành công, badge được tải lại từ API; nếu API trả `409`,
trang hiển thị thông báo cùng mã, tên, trạng thái và thời gian của các Event
đang xung đột, đồng thời giữ nguyên trạng thái venue.

Implementation progress is tracked in
`SCEAMS_IMPLEMENTATION_ROADMAP.md`.

## API danh sách sự kiện có OData

`GET /api/events` hỗ trợ `$filter`, `$orderby`, `$top`, `$skip`, `$select`,
`$expand` và `$count`. Public/Student chỉ thấy Event `Approved`; Organizer chỉ
thấy Event thuộc scope mình tạo hoặc Club mình phụ trách; Admin/Staff xem toàn
bộ Event. Response có `registeredCount` và `slotsRemaining`, trong đó chỉ
registration `Confirmed`/`Attended` được tính vào số chỗ đã dùng.

Ví dụ:

```text
GET http://localhost:5195/api/events?$filter=Status eq 'Approved'&$orderby=StartTime asc&$top=10
```

Request OData mẫu nằm trong Postman **Events** folder.

## MVC danh sách sự kiện

Mở trang:

```text
https://localhost:7034/Events
```

Trang MVC gửi OData query đã encode tới API và hỗ trợ lọc theo từ khóa (Event,
Club, venue), mã Club, khoảng ngày, status và số chỗ còn lại. Kết quả được sắp
xếp `StartTime asc`, có phân trang và hiển thị capacity/slots remaining.

## API chi tiết sự kiện

```text
GET http://localhost:5195/api/events/{id}
```

Response gồm Club, Venue, thời gian, deadline đăng ký, capacity, số đã đăng ký,
`slotsRemaining` và object `permissions` cho biết action hiện tại theo role,
ownership, status và deadline. Event Draft/Pending của Organizer khác không bị
public; API trả `404` cho URL không có quyền xem. Request mẫu nằm trong Postman
**Events** folder.

## MVC tạo Event Draft

Organizer mở form tại:

```text
https://localhost:7034/Events/Create
```

Form chỉ nạp Club Approved do Organizer hiện tại phụ trách và Venue không bảo
trì, đồng thời nhập thời gian, deadline và capacity. Tạo thành công sẽ chuyển
thẳng tới trang chi tiết Event Draft.

## API cập nhật Event

```text
PUT http://localhost:5195/api/events/{id}
Authorization: Bearer <access-token>
```

Organizer chỉ sửa Event thuộc scope của mình khi còn Draft; Admin/Staff được xử
lý theo quyền nội bộ. API kiểm tra lại Venue, khoảng thời gian, capacity và số
đăng ký hợp lệ; Event Completed/Cancelled không thể sửa.

## MVC sửa Event

Nút **Chỉnh sửa Event** chỉ xuất hiện khi API trả `permissions.canEdit = true`.
Form tại `/Events/{id}/Edit` giữ nguyên Club, cho phép cập nhật nội dung, Venue,
thời gian và capacity; lỗi ownership/status/business rule được giữ lại trên
form. Lưu thành công sẽ redirect về trang chi tiết Event.

## MVC gửi Event để duyệt

Trên trang chi tiết Event Draft, Organizer có nút **Gửi để duyệt**. MVC dùng
form POST có anti-forgery token và hộp xác nhận; sau khi API thành công, Event
được tải lại với status `PendingApproval` và nút chỉnh sửa không còn hiển thị.

## API gửi Event để duyệt

```text
PUT http://localhost:5195/api/events/{id}/submit
Authorization: Bearer <organizer-access-token>
```

Chỉ Organizer sở hữu Event được chuyển `Draft -> PendingApproval`. API kiểm tra
lại title, Club/Venue, thời gian, deadline và capacity trước khi đổi status;
Event ở trạng thái khác Draft trả `409 Conflict`.

## API queue Event chờ duyệt

Admin/Staff dùng:

```text
GET http://localhost:5195/api/events/pending-approval?clubId=1&venueId=1&page=1&pageSize=10
```

Endpoint chỉ trả Event `PendingApproval`, hỗ trợ lọc Club/Venue/ngày và phân
trang. Request mẫu nằm trong Postman **Events** folder.

## API duyệt Event và kiểm tra trùng lịch

```text
PUT http://localhost:5195/api/events/{id}/approve
Authorization: Bearer <admin-or-staff-access-token>
```

Trang `System/NotificationLog` có bộ lọc Event/type/status và nút chạy job
Development. Kết quả trả số Event quét, reminder gửi, bản ghi bỏ qua do đã có
dấu và lỗi; chạy nút lần thứ hai sẽ tăng `Skipped` thay vì tạo notification
trùng.

## ProblemDetails và lỗi API

API dùng `ApiExceptionHandlingMiddleware` cùng `ProblemDetails` cho lỗi validation,
unauthorized, forbidden, not found, conflict, not acceptable và lỗi ngoài dự
kiến. Response có `status`, `title`, `detail`, `instance` và `traceId`; production
không trả stack trace, connection string hoặc secret. Các lỗi nghiệp vụ từ
`ApiControllerBase` cũng dùng cùng format để MVC có thể xử lý thống nhất.

MVC dùng `ApiProblemDetailsParser` để đọc cả `detail` theo RFC và `message` của
các response cũ. Trang lỗi chung nằm ở `Errors/Api/{statusCode}` với partial
`Views/Shared/_ApiError.cshtml`; status `401` giữ return URL khi chuyển về Login,
còn `403` chuyển tới Access Denied.

API chỉ chuyển `PendingApproval -> Approved`, kiểm tra Venue không bảo trì và
không overlap Event `Approved/Ongoing` khác. Conflict trả `409` với danh sách
Event, Venue và khung giờ bị trùng.

## API từ chối Event

Admin/Staff từ chối Event chờ duyệt bằng:

```text
PUT http://localhost:5195/api/events/{id}/reject
Authorization: Bearer <admin-or-staff-access-token>
Content-Type: application/json
```

```json
{
  "reason": "Thời gian tổ chức chưa phù hợp với kế hoạch học kỳ."
}
```

`reason` bắt buộc từ 2 đến 500 ký tự. API chỉ cho phép chuyển
`PendingApproval -> Rejected`, lưu lý do và thời điểm xử lý; Organizer sở hữu
Event có thể xem lý do trên trang chi tiết. Request mẫu nằm trong Postman
**Events** folder.

## MVC từ chối Event

Admin/Staff có thể bấm **Từ chối Event** trên trang chi tiết để mở modal nhập
lý do. Form MVC dùng anti-forgery token, bắt buộc nội dung không rỗng (2–500 ký
tự) và xác nhận trước khi gửi. Organizer sở hữu Event sẽ thấy badge `Rejected`
và phần **Lý do từ chối** khi mở lại chi tiết.

## API hủy Event

Organizer sở hữu Event hoặc Admin/Staff dùng:

```text
PUT http://localhost:5195/api/events/{id}/cancel
Authorization: Bearer <organizer-or-admin-access-token>
Content-Type: application/json
```

```json
{
  "reason": "Lịch thi đấu của câu lạc bộ đã thay đổi."
}
```

Organizer chỉ hủy được Event của mình trước `StartTime`; Admin/Staff có thể can
thiệp theo quyền nội bộ. Event `Completed`/`Cancelled` không thể hủy lại. API
lưu `CancellationReason`, giữ nguyên Event và Registration, không hard-delete.
Request mẫu nằm trong Postman **Events** folder.

## MVC hủy Event

Trên trang chi tiết, Organizer sở hữu hoặc Admin/Staff thấy nút **Hủy Event**.
Modal hiển thị số người đăng ký hợp lệ, yêu cầu nhập lý do và xác nhận trước
khi gửi form anti-forgery. Sau khi hủy, trang hiển thị trạng thái `Cancelled`
và lý do hủy; Organizer gọi sau `StartTime` sẽ nhận thông báo lỗi từ API và
Event không đổi trạng thái.

## API đồng bộ trạng thái Event theo thời gian

API chạy `EventStatusSyncBackgroundService` theo chu kỳ cấu hình
`EventStatusSync:IntervalSeconds` (mặc định 60 giây). Job chuyển `Approved` sang
`Ongoing` khi tới `StartTime`, rồi sang `Completed` khi tới `EndTime`; mỗi lần
chạy chỉ cập nhật bản ghi cần đổi, có log số lượng chuyển trạng thái và không
đụng tới Event `Cancelled`.

Khi chạy môi trường Development, Admin có thể kích hoạt ngay bằng test hook:

```text
POST http://localhost:5195/api/events/sync-status
Authorization: Bearer <admin-access-token>
```

Endpoint bị ẩn/không khả dụng ngoài Development để tránh dùng test hook ở môi
trường production.

## MVC trạng thái Event theo thời gian

Mỗi lần mở `/Events/{id}`, MVC tải lại detail từ API nên phản ánh ngay status sau
khi background job chạy. Với Event `Ongoing`, giao diện chỉ hiển thị khu vực
Check-in cho người đã đăng ký; với Event `Completed`, giao diện hiển thị khu vực
Feedback sau khi hệ thống xác nhận điểm danh. Các endpoint thao tác thật sẽ được
kết nối ở các phase đăng ký/điểm danh/feedback tiếp theo.

## API Student đăng ký Event

Student đăng ký Event bằng:

```text
POST http://localhost:5195/api/registrations
Authorization: Bearer <student-access-token>
Content-Type: application/json
```

```json
{
  "eventId": 1
}
```

API chỉ nhận Event `Approved`, chưa quá `RegistrationDeadline`, còn chỗ và
không có registration trước đó của Student. Luồng tạo registration chạy trong
transaction `Serializable`, đếm `Confirmed`/`Attended` trước khi thêm để tránh
vượt capacity khi có nhiều request đồng thời. Thành công trả `201` với mã
registration, status `Confirmed` và số slots còn lại; lỗi hết chỗ, quá hạn hoặc
đăng ký trùng trả `409`.

## MVC đăng ký Event

Student thấy nút **Đăng ký Event** trên detail khi API xác nhận Event còn chỗ,
chưa quá deadline và Student chưa có registration. MVC gửi form POST có
anti-forgery token và hộp xác nhận; sau khi thành công tải lại detail để cập
nhật slots remaining và hiển thị trạng thái `Confirmed`. Lỗi hết chỗ, quá hạn
hoặc đăng ký trùng được hiển thị trực tiếp trên trang.

## API Student hủy đăng ký

Student dùng endpoint:

```text
PUT http://localhost:5195/api/registrations/{registrationId}/cancel
Authorization: Bearer <student-access-token>
```

Chỉ chủ sở hữu registration ở trạng thái `Confirmed` được hủy và phải còn ít
nhất 24 giờ trước `StartTime`. API đổi status sang `CancelledByStudent`, lưu
`CancelledAt`, không xóa bản ghi; slots remaining được tính lại sau khi hủy.
Nếu quá mốc 24 giờ, đã điểm danh hoặc không phải chủ sở hữu, API trả lỗi nghiệp
vụ tương ứng.

## MVC hủy đăng ký

Event detail hiển thị mã registration và nút **Hủy đăng ký** khi status là
`Confirmed`. MVC hiển thị hạn hủy (`StartTime - 24 giờ`), dùng anti-forgery và
hộp xác nhận; sau khi thành công tải lại detail để thấy status
`CancelledByStudent` và slots remaining tăng. Lỗi quá hạn, đã điểm danh hoặc
không đúng chủ sở hữu được giữ lại trên trang.

## API lịch sử đăng ký của Student

Student xem lịch sử bằng:

```text
GET http://localhost:5195/api/registrations/me?status=Confirmed&page=1&pageSize=10
Authorization: Bearer <student-access-token>
```

API lấy Student từ JWT, không nhận `studentId` từ query. Có thể lọc status
`Pending`, `Confirmed`, `Attended` hoặc `CancelledByStudent`, phân trang tối đa
50 bản ghi/trang; response gồm Event, thời gian, registration status và thông
tin điểm danh nếu đã có.

## API danh sách người đăng ký của Event

Admin hoặc Organizer sở hữu Event dùng:

```text
GET http://localhost:5195/api/events/{id}/registrations?status=Confirmed&search=SV001&page=1&pageSize=10
Authorization: Bearer <admin-or-organizer-access-token>
```

API kiểm tra ownership của Organizer, hỗ trợ lọc status, tìm theo Student code/
tên và phân trang tối đa 50 bản ghi/trang. Response chỉ trả Student code, tên,
registration status và thông tin điểm danh cần thiết; không trả email, phone hay
dữ liệu cá nhân nhạy cảm.

## MVC lịch sử đăng ký

Student mở `/Registrations` để xem trang **My Registrations**. Trang có lọc
status, phân trang, Event/thời gian, trạng thái registration và trạng thái điểm
danh. Mỗi dòng có link tới Event detail; registration `Confirmed` còn trong
thời hạn sẽ có nút hủy và dùng chung workflow anti-forgery của Event detail.

## MVC danh sách người đăng ký Event

Admin hoặc Organizer mở **Danh sách người đăng ký** từ Event detail. MVC có
filter status/search, phân trang và chỉ hiển thị Student code, tên, status cùng
thông tin điểm danh cần thiết. Organizer không sở hữu Event sẽ nhận lỗi `403`
và không xem được danh sách.

## API Organizer điểm danh

Organizer phụ trách Event dùng:

```text
PUT http://localhost:5195/api/registrations/{registrationId}/check-in
Authorization: Bearer <organizer-access-token>
```

API chỉ cho điểm danh registration `Confirmed` của Event đang `Ongoing` và
đúng khung `StartTime`–`EndTime`. Luồng dùng transaction Serializable, tạo tối
đa một Attendance và chuyển registration sang `Attended`; điểm danh trùng hoặc
ngoài thời gian cho phép trả `409`.

## MVC điểm danh

Trong danh sách registration của Event, Organizer thấy nút **Check-in** cho
registration `Confirmed`. Nút bị khóa ngay khi submit để chống double-click;
sau khi thành công danh sách tải lại với status `Attended`, thời gian và người
check-in. API từ chối check-in trùng nên không tạo Attendance thứ hai.

## API Student gửi Feedback

Student đã điểm danh dùng:

```text
POST http://localhost:5195/api/events/{id}/feedback
Authorization: Bearer <student-access-token>
Content-Type: application/json
```

```json
{
  "rating": 5,
  "comment": "Nội dung hữu ích và tổ chức tốt."
}
```

API chỉ cho Student có registration `Attended` gửi feedback, mỗi Student tối đa
một feedback/Event. Rating bắt buộc 1–5, comment tối đa 2.000 ký tự; gửi trùng
hoặc chưa điểm danh trả `409`.

## MVC gửi Feedback

Event detail chỉ hiển thị form Feedback khi API trả `canFeedback = true` (Student
đã `Attended` và chưa gửi trước đó). Form có rating 1–5, comment tối đa 2.000
ký tự và anti-forgery token. Sau khi gửi thành công, form bị thay bằng feedback
đã lưu; lỗi chưa điểm danh hoặc gửi trùng được hiển thị trên trang.

## API tổng hợp Feedback

Public hoặc user có quyền xem Event dùng:

```text
GET http://localhost:5195/api/events/{id}/feedback?page=1&pageSize=10
```

API trả `averageRating`, `totalFeedback` và danh sách feedback phân trang. Chỉ
rating, comment và thời gian tạo được trả về; không lộ StudentId, email hay
thông tin cá nhân. Event Draft/Pending của Organizer khác không được public.

## MVC hiển thị Feedback

Event detail tải summary từ API và hiển thị average rating, tổng số feedback và
danh sách rating/comment/thời gian. Khi chưa có đánh giá, trang có empty state;
sau khi Student gửi feedback và tải lại detail, average cùng danh sách được cập
nhật.

## MVC queue duyệt Event

Admin/Staff mở trang:

```text
https://localhost:7034/Events/Pending
```

Queue có filter Club, Venue, date range, phân trang và link tới chi tiết Event
trước khi quyết định. Admin/Staff có thể bấm **Approve** ngay trên queue hoặc
trang chi tiết; MVC dùng form POST có anti-forgery token và hộp xác nhận.

Nếu lịch Venue bị overlap, API trả `409 Conflict` và MVC hiển thị từng Event
xung đột cùng Venue, trạng thái và khung giờ ngay trên trang chi tiết. Khi duyệt
thành công, Event chuyển sang `Approved`, vì vậy Student có thể nhìn thấy Event
trong danh sách công khai và đăng ký.

## API Organizer tạo Event Draft

Organizer tạo Event bằng:

```text
POST http://localhost:5195/api/events
Authorization: Bearer <organizer-access-token>
Content-Type: application/json
```

API chỉ nhận Club Approved thuộc Organizer, Venue không bảo trì, thời gian hợp
lệ (`StartTime < EndTime`, deadline không sau StartTime) và capacity không vượt
sức chứa Venue. Event mới luôn có status `Draft`; lỗi ownership, maintenance
hoặc dữ liệu thời gian trả status phù hợp. Request mẫu nằm trong Postman
**Events** folder.

## MVC chi tiết sự kiện

Từ danh sách Event chọn tiêu đề hoặc mở trực tiếp:

```text
https://localhost:7034/Events/{id}
```

Trang hiển thị Club, Venue, thời gian, deadline, sức chứa, lý do từ chối/hủy
và các action được API cấp quyền. Direct URL tới Event Draft/Pending của người
khác hiển thị 404 và không lộ dữ liệu.

## API báo cáo hoạt động CLB

Admin/Staff xem toàn bộ hoạt động; Organizer chỉ xem các Club thuộc quyền phụ
trách:

```text
GET http://localhost:5195/api/reports/club-activity?from=2026-01-01&to=2026-12-31
Authorization: Bearer <admin-or-organizer-access-token>
```

Response trả theo từng Club: số Event, số registration hợp lệ, số lượt đã
điểm danh và rating trung bình. Khoảng ngày lọc theo `StartTime` của Event;
`from` và `to` là tùy chọn, còn ngày `to` được tính trọn ngày.

## API báo cáo tỷ lệ tham dự

Admin/Staff xem toàn bộ Event; Organizer chỉ xem các Event thuộc Club mình phụ
trách:

```text
GET http://localhost:5195/api/reports/attendance-rate?from=2026-01-01&to=2026-12-31
Authorization: Bearer <admin-or-organizer-access-token>
```

Mỗi dòng báo cáo trả số registration hợp lệ, số lượt đã điểm danh và tỷ lệ
`Attended / Confirmed * 100`. Event chưa có registration trả tỷ lệ `0`, không
phát sinh lỗi chia cho 0. Khoảng ngày được lọc theo `StartTime` và ngày `to`
được tính trọn ngày.

## API báo cáo sử dụng Venue

Admin/Staff có thể xem số Event và tổng thời lượng sử dụng theo từng Venue:

```text
GET http://localhost:5195/api/reports/venue-usage?from=2026-01-01&to=2026-12-31
Authorization: Bearer <admin-or-staff-access-token>
```

Báo cáo chỉ tính Event đã được duyệt, đang diễn ra hoặc đã hoàn thành; tổng
thời lượng được tính theo giờ từ `StartTime` đến `EndTime`. Ngày `to` bao gồm
toàn bộ ngày được chọn.

## Content negotiation JSON/XML

`GET /api/events` hỗ trợ chọn định dạng bằng header `Accept`:

```text
GET https://localhost:5195/api/events?$top=10
Accept: application/json
```

Hoặc:

```text
GET https://localhost:5195/api/events?$top=10
Accept: application/xml
```

API dùng response DTO cho danh sách Event và trả `406 Not Acceptable` nếu client
yêu cầu định dạng không được hỗ trợ.

Trong môi trường Development, Admin/Staff có thể mở:

```text
https://localhost:7034/System/ContentNegotiation
```

Trang này gửi `Accept` từ MVC tới API, hiển thị raw response đã được Razor
escape và có lựa chọn `text/csv` để kiểm tra luồng `406 Not Acceptable`.

## Notification gRPC

`SCEAMS.NotificationService` cung cấp RPC `PublishEventNotification`. API tạo
correlation ID cho mỗi lần chuyển Event sang `Approved` hoặc `Cancelled`, gọi
gRPC qua `INotificationClientService` và trả correlation ID trong response Event.
Client có timeout 3 giây, tối đa một lần retry; mọi retry giữ nguyên correlation
ID nên server có thể deduplicate. Địa chỉ mặc định là
`https://localhost:7001`, có thể thay bằng `NotificationGrpc:Address` trong
User Secrets hoặc biến môi trường.

Admin/Staff có thể xem log giả lập ở môi trường Development:

```text
https://localhost:7034/System/NotificationLog
```

Trang hiển thị Event, loại notification, correlation ID, thời gian và lỗi nếu
gRPC đang tạm dừng. Khi gRPC chạy lại, workflow Approve/Cancel tiếp tục tạo log
thành công.

## Reminder deadline đăng ký

API chạy `EventReminderBackgroundService` theo chu kỳ và tìm Event `Approved`
có `RegistrationDeadline` trong 24 giờ tới. Dấu gửi được lưu trong bảng
`NotificationDeliveries` với unique key `(EventId, NotificationType)`, vì vậy
chạy job nhiều lần không tạo reminder trùng. Trong Development, Admin/Staff có
thể chạy thủ công:

```text
POST https://localhost:7069/api/reminders/run
Authorization: Bearer <admin-or-staff-access-token>
```

## AI FAQ — Phase 121: Retrieval Event không dùng AI

Student có thể gửi câu hỏi tự nhiên tới endpoint retrieval để lấy tối đa 10
Event `Approved` sắp xếp theo `StartTime`. Retrieval không gọi AI provider và
không trả Event `Draft`, `PendingApproval` hoặc Event đã bắt đầu.

```text
POST https://localhost:7069/api/chatbot/retrieval
Authorization: Bearer <student-access-token>
Content-Type: application/json

{
  "question": "Workshop AI hôm nay còn chỗ"
}
```

Parser hỗ trợ keyword theo title/description/club/venue, mốc `hôm nay`, `tuần
này`, `tháng này` và điều kiện `còn chỗ`/`còn slot`. Mốc ngày được tính theo
`Asia/Ho_Chi_Minh`, còn thời gian trong database là UTC. Response gồm
`title`, `clubName`, `venueName`, `startTime`, `endTime`, `capacity`,
`registeredCount` và `slotsRemaining`. Câu hỏi không có kết quả vẫn trả `200`
với `relatedEvents: []` để MVC hiển thị empty state.

Student đăng nhập có thể mở trang MVC:

```text
https://localhost:7034/Chatbot
```

Trang dùng typed `IEventFaqApiClient`, tự gắn Bearer token qua handler dùng
chung, có anti-forgery token, loading feedback khi submit, empty state và lỗi
kết nối/401/403 rõ ràng. Các liên kết Event trên kết quả đi tới trang chi tiết
để Student tiếp tục đăng ký.

### Phase 123 — Sinh câu trả lời bằng AI provider

Khi đã cấu hình provider, Student có thể gọi:

```text
POST https://localhost:7069/api/chatbot/ask
Authorization: Bearer <student-access-token>
Content-Type: application/json

{
  "question": "Workshop AI hôm nay còn chỗ không?"
}
```

`AiChatService` luôn chạy retrieval trước, chỉ đưa title/club/venue/time/capacity
và slots còn lại của Event Approved vào context. Nếu context rỗng, API trả câu
“Không tìm thấy Event Approved phù hợp...” và không gọi provider. Nếu provider
chưa cấu hình hoặc không khả dụng, API trả `503` mà không lộ API key, prompt
hoặc stack trace.

Provider được cấu hình bằng User Secrets/environment variables (không commit
key thật):

```text
AI__Enabled=true
AI__Endpoint=<provider-chat-completions-endpoint>
AI__Model=<provider-model>
AI__ApiKey=<secret>
AI__TimeoutSeconds=15
```

Trang MVC `/Chatbot` gửi câu hỏi tới `POST /api/chatbot/ask`, hiển thị câu trả
lời và link Event liên quan. Khi request đang chạy, nút submit bị vô hiệu hóa
và đổi nhãn; timeout, `503` provider unavailable, lỗi API và context rỗng đều
có trạng thái hiển thị riêng.

### Phase 125 — Lịch sử chatbot

Mỗi câu trả lời thành công (kể cả câu hỏi không có Event phù hợp) được lưu vào
bảng `ChatLogs` với `Question`, `AnswerText`, danh sách ID Event liên quan,
Student lấy từ claim JWT và `CreatedAt` UTC. Student có thể xem lịch sử của
chính mình bằng endpoint phân trang:

```text
GET https://localhost:7069/api/chatbot/history?page=1&pageSize=10
Authorization: Bearer <student-access-token>
```

Server bỏ qua mọi Student ID nếu client cố gửi lên; query luôn dùng Student ID
trong JWT và giới hạn `pageSize` tối đa 50.

MVC hiển thị lịch sử tại:

```text
https://localhost:7034/Chatbot/History
```

Trang có empty/error state, phân trang và link tới Event liên quan. Student A
không thể đổi query để đọc log của Student B vì API không nhận Student ID từ
client.

### Phase 127 — Giới hạn câu hỏi

API đếm các chat log thành công của Student trong cửa sổ trượt một giờ. Từ câu
hỏi thứ 11, `POST /api/chatbot/ask` trả `429 Too Many Requests`, header
`Retry-After` tính theo chat log cũ nhất và không chạy retrieval/provider.

MVC đọc header này tại `/Chatbot`, hiển thị countdown và vô hiệu hóa nút hỏi
trong thời gian chờ; khi hết thời gian, form tự mở lại để Student thử câu hỏi
mới.
