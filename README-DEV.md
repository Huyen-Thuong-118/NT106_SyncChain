# SyncChain — Developer Guide

Local development workflow for the NT106_SyncChain solution: an ASP.NET Core Web
API (`SyncChain.API`) backed by PostgreSQL, and a .NET MAUI desktop client
(`app/SyncChain.Desktop`).

---

## 1. Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| .NET SDK | 10.0.x | `dotnet --version` |
| MAUI workload | `maui-windows` | `dotnet workload list` |
| PostgreSQL | any reachable instance (local **or** Neon cloud) | see `.env` |

Install the MAUI workload if missing:

```powershell
dotnet workload install maui
```

---

## 2. One-time setup

1. Copy the environment template and set your database connection string:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Edit `.env`. Exactly **one** `DATABASE_URL` line must be active (the API and
   the scripts both use the *first* non-comment `DATABASE_URL`):

   ```
   # Cloud (Neon) — no local install needed:
   DATABASE_URL=postgresql://<user>:<pass>@<host>/<db>?sslmode=require

   # Local PostgreSQL (comment the line above, uncomment this):
   # DATABASE_URL=postgresql://postgres:<pass>@localhost:5432/syncchain
   ```

   > The API creates the schema and seed data automatically on startup
   > (`EnsureCreated` + roles + an `admin@gmail.com` account). For a **local**
   > database you only need the empty `syncchain` database to exist first
   > (`CREATE DATABASE syncchain;`). Neon works out of the box.

---

## 3. Run it

### Everything at once (recommended)

```powershell
.\run-all.bat          # or:  .\scripts\run-all.ps1   (or legacy: .\run.ps1)
```

This will:

1. Check the database is reachable (from `DATABASE_URL`).
2. Start the **backend in its own window** (logs stay visible).
3. Wait until `GET /health` returns `200`.
4. Start the **Desktop app** in the current window.

### Individual pieces

```powershell
.\run-backend.bat      # restore + build + run the API only
.\run-frontend.bat     # restore + build + run the Desktop app only
.\scripts\run-database.ps1   # verify DB connectivity only
```

---

## 4. Default accounts

| Role | Email | Password | Login button |
|------|-------|----------|--------------|
| admin | `admin@gmail.com` | `123456` | **Đăng nhập** (admin portal) |
| customer | *self-register in the app* | — | **Đăng nhập khách hàng** |

Customer accounts are created through the in-app **Đăng ký** screen, which now
calls the real `POST /api/Auth/register` endpoint, auto-logs-in, and opens the
customer shell.

---

## 5. Health & observability

- **Backend health:** `GET http://localhost:5292/health`
  → `{ "status": "healthy", "database": "connected" }` (503 if the DB is down).
  The Desktop Login screen probes this on load and shows a friendly message if
  the server is not ready — it never crashes.
- **Swagger:** `http://localhost:5292/swagger`
- **Backend logs** (in the backend window):
  - `[Startup] ...` — DB connect / schema / seed / ready
  - `[HTTP] <METHOD> <path> -> <status> (<ms>)` — every request
  - `[Auth] ...` — register / login / password check / JWT generation
- **Desktop logs** (Debug output / console):
  - `[Desktop/Login] ...`, `[Desktop/Register] ...`

Example of a successful login trace:

```
[Desktop/Login] Nút đăng nhập được bấm ...
[Desktop/Login] Gọi POST api/Auth/login cho user@example.com
[Auth]  Nhận yêu cầu đăng nhập cho user@example.com
[Auth]  Tìm thấy user id=6, mật khẩu hợp lệ, vai trò=customer
[Auth]  Đã sinh JWT cho user id=6 (hết hạn sau 2 giờ)
[HTTP]  POST /api/Auth/login -> 200 (120 ms)
[Desktop/Login] Đăng nhập OK (role=customer), token đã nhận
```

---

## 6. Project layout

```
SyncChain.API/                ASP.NET Core Web API
  Configuration/              EnvFileLoader (.env -> connection string)
  Controllers/                HTTP endpoints (AuthController, ...)
  Services/                   Business logic (AuthService, OrderService, ...)
  Data/AppDbContext.cs        EF Core DbContext (Npgsql / PostgreSQL)
  Models/  DTOs/              Entities and request/response contracts
  Program.cs                  Startup, DI, JWT, /health, logging, seed

app/SyncChain.Desktop/        .NET MAUI client
  Services/
    ApiClientProvider.cs      Single shared HttpClient + token/session + /health
    SessionGuard.cs           Central 401 handling -> back to Login
    AppLog.cs                 Consistent [Desktop/...] logging
  Views/Pages/                Login, Register, Orders, ...

scripts/                      Dev workflow
  _common.ps1                 Shared helpers (DB check, health wait, colors)
  run-database.ps1  run-backend.ps1  run-frontend.ps1  run-all.ps1
run-all.bat  run-backend.bat  run-frontend.bat        .bat wrappers
.env / .env.example           Environment-based configuration (git-ignored)
```

---

## 7. Troubleshooting

| Symptom | Cause / fix |
|---------|-------------|
| Login says *"Sai thông tin đăng nhập"* right after registering | You must register **through the app** (real API call). The old build had a mock register that saved nothing. |
| *"Máy chủ chưa sẵn sàng"* on the Login screen | Backend not running — start `run-backend.bat`, or check its window for a DB error. |
| Backend window exits immediately | `DATABASE_URL` wrong or DB unreachable; for local PG make sure the `syncchain` database exists. |
| `run-all.ps1` reports *"Backend did not become healthy"* | Read the backend window — the real error (DB/JWT/port) is printed there. |
| Port `5292` already in use | An old API instance is still running — close its window or stop the `SyncChain.API` process. |
