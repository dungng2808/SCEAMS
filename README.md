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

Implementation progress is tracked in
`SCEAMS_IMPLEMENTATION_ROADMAP.md`.
