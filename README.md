# SyncChain — Hệ thống quản lý vận hành chuỗi bán lẻ

> Đồ án môn **NT106 – Lập trình mạng căn bản**.
> Hệ thống Client–Server quản lý bán lẻ: quản trị sản phẩm/kho/đơn hàng cho nhân
> sự nội bộ và cổng mua sắm cho khách hàng, có xác thực JWT, thanh toán
> (COD/VNPay/MoMo) và cập nhật thời gian thực qua SignalR.

---

## 1. Thành viên

| Họ tên | MSSV | Vai trò |
|--------|------|---------|
| Nguyễn Đỗ Ngọc Huyền Thương | 24521750 | |
| Tăng Thanh Thư | 245217xx | |
| Mai Lương Khánh Vy | 24521xxx | |

---

## 2. Tổng quan

SyncChain gồm **hai phần chạy độc lập**:

1. **Ứng dụng chính (bản demo & chấm điểm):**
   - **`SyncChain.API`** — Web API viết bằng **ASP.NET Core (.NET 10)**.
   - **`app/SyncChain.Desktop`** — ứng dụng **.NET MAUI** chạy trên Windows.
   - Client MAUI gọi trực tiếp tới `SyncChain.API` (mặc định `http://localhost:5292`).

2. **Bản web prototype (baseline NT106, tùy chọn):**
   - **`src/`** — backend **Node.js + Express** (cổng `3000`).
   - **`ui/`** — giao diện web HTML/CSS/JS.
   - Đây là phiên bản đầu của đồ án; **không bắt buộc** để chạy ứng dụng desktop.

> Cả hai phần dùng **chung một cơ sở dữ liệu PostgreSQL** (Neon cloud hoặc local),
> đọc cùng một biến `DATABASE_URL` trong file `.env`.

---

## 3. Kiến trúc hệ thống

```
                       ┌─────────────────────────────┐
                       │   SyncChain.Desktop (MAUI)   │   ← Ứng dụng khách trên Windows
                       │  ┌───────────┐ ┌───────────┐ │
                       │  │ AppShell  │ │CustomerShell│ │   AppShell: admin/manager/staff
                       │  │ (nội bộ)  │ │ (khách)   │ │   CustomerShell: khách hàng
                       │  └───────────┘ └───────────┘ │
                       └───────┬──────────────┬────────┘
              HTTP + JWT Bearer │              │ SignalR (WebSocket)
                                ▼              ▼
                       ┌─────────────────────────────┐
                       │   SyncChain.API (.NET 10)    │   http://localhost:5292
                       │  Controllers → Services →    │   /health, /swagger
                       │  EF Core (AppDbContext)      │   Hubs: /hubs/chat, /hubs/order
                       └──────────────┬──────────────┘
                                      │ Npgsql (SSL)
                                      ▼
                       ┌─────────────────────────────┐
                       │   PostgreSQL (Neon / local)  │   DB: syncchain
                       └─────────────────────────────┘

        (Tùy chọn) Bản web prototype:  ui/ (HTML)  →  src/ Express (:3000)  →  PostgreSQL
```

**Luồng một yêu cầu (desktop):**
`Trang MAUI → HttpClient (kèm Bearer token) → Controller → Service → EF Core (AppDbContext) → PostgreSQL`,
và chiều ngược lại cho realtime: `Service → SignalR Hub → Client MAUI` (thông báo đơn/thanh toán).

**Điểm chính về backend .NET:**
- Xác thực bằng **JWT**; phân quyền theo **policy** dựa trên vai trò.
- Khởi động sẽ **tự tạo schema + seed dữ liệu** (không cần chạy migration tay).
- Có **`/health`** (kiểm tra kết nối DB) và **logging có cấu trúc** (`[Auth]`, `[HTTP]`, `[Startup]`).

---

## 4. Công nghệ & công cụ

### Backend chính — `SyncChain.API`
| Hạng mục | Công nghệ |
|----------|-----------|
| Nền tảng | ASP.NET Core Web API, **.NET 10** |
| ORM / DB | Entity Framework Core 10 + **Npgsql** (PostgreSQL) |
| Xác thực | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Mật khẩu | **BCrypt.Net-Next** (băm mật khẩu) |
| Realtime | **SignalR** (hub chat, hub đơn hàng, đẩy thông báo/thanh toán) |
| Tài liệu API | **Swagger** (Swashbuckle) tại `/swagger` |
| Thanh toán | **COD**, **VNPay**, **MoMo** (sandbox) |
| Khác | Email (SMTP), Audit log, System error log, quản lý kho/vận chuyển/báo cáo |

### Client — `app/SyncChain.Desktop`
| Hạng mục | Công nghệ |
|----------|-----------|
| Nền tảng | **.NET MAUI** (`net10.0-windows10.0.19041.0`), unpackaged |
| UI | Microsoft.Maui.Controls (XAML) |
| Kết nối | 1 `HttpClient` dùng chung (`ApiClientProvider`) + JWT Bearer |
| Realtime | `Microsoft.AspNetCore.SignalR.Client` |

### Bản web prototype (tùy chọn) — `src/` + `ui/`
| Hạng mục | Công nghệ |
|----------|-----------|
| Backend | Node.js 18+, **Express 5**, `pg`, `dotenv`, `cors` (cổng `3000`) |
| Frontend | HTML / CSS / JavaScript thuần |

### Công cụ phát triển
| Hạng mục | Công cụ |
|----------|---------|
| Chạy dự án | PowerShell scripts trong `scripts/` + wrapper `*.bat` |
| Cấu hình | `.env` (biến `DATABASE_URL`) ở thư mục gốc |
| CSDL cloud | [Neon](https://neon.tech) (PostgreSQL serverless) |
| IDE | Visual Studio 2022 / VS Code / Rider |

---

## 5. Cấu trúc thư mục

```
NT106_SyncChain/
├── SyncChain.API/                 # ★ Backend chính (ASP.NET Core .NET 10)
│   ├── Controllers/               #   HTTP endpoints (Auth, Order, Cart, Product, Payment...)
│   ├── Services/                  #   Nghiệp vụ (AuthService, OrderService, CartService...)
│   ├── Data/AppDbContext.cs       #   EF Core DbContext (PostgreSQL)
│   ├── Models/  DTOs/             #   Entity + hợp đồng request/response
│   ├── Configuration/EnvFileLoader.cs  # Nạp .env → chuỗi kết nối
│   ├── Hubs/                      #   SignalR (ChatHub, OrderHub)
│   ├── Program.cs                 #   Khởi động, DI, JWT, /health, logging, seed
│   └── appsettings.json           #   Cấu hình JWT / Email / VNPay / MoMo
│
├── app/SyncChain.Desktop/         # ★ Client MAUI (Windows)
│   ├── Services/
│   │   ├── ApiClientProvider.cs   #   HttpClient dùng chung + token + /health
│   │   ├── SessionGuard.cs        #   Xử lý 401 tập trung → về Login
│   │   ├── OrderStatusDisplay.cs  #   Nguồn hiển thị trạng thái đơn (dùng chung)
│   │   └── AppLog.cs              #   Logging [Desktop/...]
│   ├── Views/Pages/               #   Login, Register, Customer*, Product*, Order*...
│   ├── AppShell / CustomerShell   #   2 shell theo vai trò
│   └── MauiProgram.cs
│
├── scripts/                       # Script phát triển
│   ├── _common.ps1                #   Hàm dùng chung (check DB, chờ /health, màu)
│   ├── run-database.ps1  run-backend.ps1  run-frontend.ps1  run-all.ps1
├── run-all.bat  run-backend.bat  run-frontend.bat            # Wrapper cho Windows
├── run.ps1                        # Alias gọi scripts/run-all.ps1
│
├── src/                           # (Tùy chọn) Backend web prototype (Node + Express)
├── ui/                            # (Tùy chọn) Giao diện web prototype (HTML)
├── database/TaoBang.sql           # Backup schema PostgreSQL (tham khảo)
├── .env / .env.example            # Cấu hình DATABASE_URL (bị gitignore)
├── README.md  README-DEV.md       # Tài liệu (bản này + hướng dẫn dev chi tiết)
└── NT106_SyncChain.sln
```

---

## 6. Yêu cầu môi trường

| Bắt buộc cho ứng dụng chính | Kiểm tra |
|-----------------------------|----------|
| **.NET SDK 10.0.x** | `dotnet --version` |
| **Workload `maui-windows`** | `dotnet workload list` → cài: `dotnet workload install maui` |
| **PostgreSQL** đang chạy (Neon cloud **hoặc** local) | xem mục cấu hình |
| Windows 10/11 (cho MAUI desktop) | |

| Tùy chọn (chỉ khi chạy bản web prototype) | |
|-------------------------------------------|--|
| **Node.js 18+** | `node --version` |

---

## 7. Cấu hình

Toàn bộ cấu hình kết nối DB nằm ở **một file `.env` duy nhất** tại thư mục gốc.

### Bước 1 — Tạo `.env`
```powershell
Copy-Item .env.example .env
```

### Bước 2 — Điền `DATABASE_URL`
Trong `.env`, để **đúng một** dòng `DATABASE_URL` không phải comment (dòng đầu tiên
sẽ được dùng):

```env
# Dùng Neon (cloud) — không cần cài PostgreSQL local:
DATABASE_URL=postgresql://<user>:<password>@<host>.neon.tech/syncchain?sslmode=require

# Hoặc PostgreSQL local (comment dòng Neon ở trên, bỏ comment dòng này):
# DATABASE_URL=postgresql://postgres:<password>@localhost:5432/syncchain
```

> **Quan trọng:**
> - Backend .NET đọc `DATABASE_URL` từ `.env` (qua `EnvFileLoader`), **không** dùng
>   `appsettings.json → ConnectionStrings`. Không cần sửa `appsettings.json` để đổi DB.
> - Khi khởi động, API **tự tạo bảng + seed** (roles + tài khoản admin). Với **PostgreSQL
>   local** chỉ cần tạo sẵn database rỗng: `CREATE DATABASE syncchain;`. **Neon** dùng được ngay.
> - Các cấu hình **JWT / Email / VNPay / MoMo** nằm trong `SyncChain.API/appsettings.json`
>   (đã có sẵn giá trị sandbox để demo).

---

## 8. Chạy toàn bộ ứng dụng (nhanh nhất)

```powershell
.\run-all.bat          # hoặc: .\scripts\run-all.ps1   (hoặc alias: .\run.ps1)
```

Script sẽ tự động theo đúng thứ tự:

1. **Kiểm tra PostgreSQL** (đọc `DATABASE_URL`, thử kết nối).
2. **Chạy backend** trong một cửa sổ riêng (log hiển thị liên tục).
3. **Chờ tới khi `GET /health` trả `200`** (DB kết nối OK, schema/seed xong).
4. **Chạy ứng dụng Desktop** trong cửa sổ hiện tại.

Mỗi cửa sổ giữ nguyên với log màu để dễ theo dõi/gỡ lỗi.

---

## 9. Chạy từng phần

### 9.1. Chỉ backend .NET
```powershell
.\run-backend.bat          # restore + build + run SyncChain.API
```
- API: `http://localhost:5292`
- Health: `http://localhost:5292/health`
- Swagger: `http://localhost:5292/swagger`

### 9.2. Chỉ client MAUI
```powershell
.\run-frontend.bat         # restore + build + run app/SyncChain.Desktop
```
> Nếu backend chưa chạy, màn hình Đăng nhập sẽ báo *"Máy chủ chưa sẵn sàng"* (không crash).

### 9.3. Chỉ kiểm tra database
```powershell
.\scripts\run-database.ps1 # xác nhận kết nối tới DB trong .env
```

### 9.4. Chạy thủ công (không dùng script)
```powershell
# Backend
dotnet run --project SyncChain.API
# Desktop (cửa sổ khác)
dotnet run --project app/SyncChain.Desktop -f net10.0-windows10.0.19041.0
```
Hoặc mở `NT106_SyncChain.sln` bằng **Visual Studio 2022** và nhấn **F5**.

### 9.5. (Tùy chọn) Bản web prototype Node + web UI
```powershell
npm install
npm start                  # Express chạy tại http://localhost:3000
```
> Mở các trang trong `ui/` để dùng giao diện web. Phần này độc lập với ứng dụng MAUI.

---

## 10. Tài khoản & phân quyền

| Vai trò | Tài khoản | Đăng nhập bằng |
|---------|-----------|----------------|
| **admin** | `admin@gmail.com` / `123456` (seed sẵn) | nút **Đăng nhập** (cổng quản trị) |
| **customer** | tự **Đăng ký** trong app | nút **Đăng nhập khách hàng** |
| staff / manager | do admin tạo trong mục Quản lý người dùng | nút Đăng nhập |

- **Cổng quản trị (AppShell):** dashboard, sản phẩm, kho, đơn hàng, người dùng, log... — cho `admin/manager/staff` (hiển thị theo vai trò).
- **Cổng khách hàng (CustomerShell):** trang chủ, sản phẩm, giỏ hàng, đơn hàng, địa chỉ, hồ sơ, thông báo — cho `customer`.

---

## 11. Luồng nghiệp vụ khách hàng (demo)

```
Đăng ký / Đăng nhập → Duyệt sản phẩm → Thêm vào giỏ → Giỏ hàng
   → Đặt hàng (chọn địa chỉ giao) → Thanh toán (COD/VNPay/MoMo)
   → Theo dõi đơn (timeline realtime) → Đơn của tôi → (Tự hủy khi còn chờ duyệt)
```

- Đặt hàng lấy thông tin người nhận từ **sổ địa chỉ** của khách (`MaDiaChi`), server tự nạp.
- Thanh toán **COD** ghi nhận ngay; **VNPay/MoMo** mở trình duyệt sandbox, kết quả đẩy về app qua SignalR.
- Khách chỉ xem/hủy được **đơn của chính mình** (kiểm soát quyền phía server).

---

## 12. Trạng thái đơn hàng

Bộ trạng thái chuẩn (một nguồn duy nhất ở backend `OrderStatuses.cs`, client hiển thị qua `OrderStatusDisplay`):

| Mã (backend) | Hiển thị | Ý nghĩa |
|--------------|----------|---------|
| `pending` | Chờ duyệt | Đơn vừa tạo, chờ nhân sự xử lý |
| `processing` | Đang xử lý | Đã tiếp nhận/chuẩn bị |
| `shipping` | Đang giao | Đã tạo vận chuyển |
| `done` | Hoàn thành | Khách đã nhận |
| `cancel` | Đã hủy | Đơn bị hủy (hoàn kho) |

Chuyển trạng thái hợp lệ: `pending → processing → shipping → done`; có thể `cancel` ở `pending/processing/shipping`.

---

## 13. Quan sát & gỡ lỗi

- **Health:** `GET http://localhost:5292/health` → `{ "status": "healthy", "database": "connected" }` (503 nếu DB lỗi).
- **Swagger:** `http://localhost:5292/swagger` để thử API trực tiếp.
- **Log backend** (trong cửa sổ backend):
  - `[Startup]` — kết nối DB / tạo schema / seed / sẵn sàng
  - `[HTTP] <METHOD> <path> -> <status> (<ms>)` — mọi request
  - `[Auth]` — đăng ký / đăng nhập / kiểm tra mật khẩu / sinh JWT
- **Log client** (Debug output / console): `[Desktop/Login]`, `[Desktop/Register]`...
- **Đổi địa chỉ API cho client:** đặt biến môi trường `SYNCCHAIN_API_URL` (mặc định `http://localhost:5292/`).

Chi tiết quy trình dev, ví dụ log một lần đăng nhập, và bảng sự cố xem thêm ở **[README-DEV.md](README-DEV.md)**.

---

## 14. Sự cố thường gặp

| Hiện tượng | Nguyên nhân / cách xử lý |
|------------|--------------------------|
| Login báo *"Sai thông tin đăng nhập"* ngay sau khi đăng ký | Phải đăng ký **trong app** (gọi API thật). Dùng đúng cổng đăng nhập theo vai trò. |
| *"Máy chủ chưa sẵn sàng"* ở màn hình Đăng nhập | Backend chưa chạy — chạy `run-backend.bat`, hoặc xem cửa sổ backend có lỗi DB không. |
| Cửa sổ backend tắt ngay | `DATABASE_URL` sai hoặc DB không kết nối được; với local nhớ tạo sẵn database `syncchain`. |
| `run-all` báo *"Backend did not become healthy"* | Xem log ở cửa sổ backend — lỗi thật (DB/JWT/cổng) nằm ở đó. |
| Cổng `5292` đang bận | Còn tiến trình `SyncChain.API` cũ — đóng cửa sổ đó hoặc kết thúc tiến trình. |
| Build MAUI báo file `.exe` bị khóa | App đang chạy — đóng ứng dụng Desktop rồi build lại. |

---

## 15. Ghi chú bảo mật

- `.env` chứa thông tin kết nối DB và **đã bị `.gitignore`** — mỗi người tự tạo từ `.env.example`, không commit.
- Không đưa mật khẩu/token thật vào mã nguồn hay README công khai.
- `SyncChain.API/appsettings.json` chỉ chứa khóa JWT demo và cấu hình sandbox — đổi khóa thật khi triển khai production.

---

## 16. Giấy phép

Xem file [LICENSE](LICENSE).
