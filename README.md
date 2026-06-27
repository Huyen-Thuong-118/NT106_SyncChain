# SyncChain — Hệ thống quản lý vận hành chuỗi bán lẻ

Đồ án môn NT106 - Lập trình mạng căn bản.  
Kiến trúc Client-Server gồm Node.js backend, .NET 8 Web API, và MAUI Desktop client, sử dụng PostgreSQL (cloud Neon).

---

## Thành viên

| Họ tên | MSSV | Vai trò |
|--------|------|---------|
| Nguyễn Đỗ Ngọc Huyền Thương | 24521750 | |
| Tăng Thanh Thư | 245217xx | |
| Mai Lương Khánh Vy | 24521xxx | |

---

## Cây thư mục

```
NT106_SyncChain/
├── src/                        # Node.js backend (Express + Socket.IO)
│   ├── controllers/
│   ├── routes/
│   ├── models/
│   └── server.js
├── SyncChain.API/              # .NET 8 Web API (xác thực, quản lý sản phẩm...)
├── app/SyncChain.Desktop/      # MAUI Desktop client
├── database/
│   └── TaoBang.sql             # Backup schema PostgreSQL (tham khảo)
├── .env.example                # Mẫu cấu hình biến môi trường Node.js
└── package.json
```

---

## Yêu cầu cài đặt

- [Node.js 18+](https://nodejs.org)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- .NET MAUI workload:
  ```bash
  dotnet workload install maui
  ```
- Tài khoản [Neon](https://neon.tech) — database PostgreSQL đã được host sẵn trên cloud, **không cần cài PostgreSQL local**

---

## Hướng dẫn chạy

### Bước 1 — Clone repo

```bash
git clone <url-repo>
cd NT106_SyncChain
```

### Bước 2 — Lấy connection string từ Neon

1. Đăng nhập [neon.tech](https://neon.tech) → vào project **SyncChain**
2. Chọn **Dashboard** → **Connection string**
3. Chọn định dạng **psql / connection URL**, copy chuỗi có dạng:
   ```
   postgresql://user:password@ep-xxxx.us-east-2.aws.neon.tech/syncchain?sslmode=require
   ```

> Liên hệ nhóm trưởng để được cấp quyền truy cập vào Neon project.

### Bước 3 — Cấu hình Node.js backend

Tạo file `.env` từ file mẫu:

```bash
cp .env.example .env
```

Mở `.env`, dán connection string Neon vào:

```env
DATABASE_URL=postgresql://user:password@ep-xxxx.us-east-2.aws.neon.tech/syncchain?sslmode=require
```

### Bước 4 — Cấu hình .NET API

Trước tiên, chạy lệnh này để git **bỏ qua thay đổi local** của file cấu hình (chỉ cần chạy một lần sau khi clone):

```bash
git update-index --skip-worktree SyncChain.API/appsettings.json
```

Sau đó mở `SyncChain.API/appsettings.json`, sửa phần `ConnectionStrings.Default` thành connection string Neon của nhóm:

```json
"ConnectionStrings": {
  "Default": "Host=ep-xxxx.us-east-2.aws.neon.tech;Database=syncchain;Username=user;Password=password;SSL Mode=Require;Trust Server Certificate=true"
}
```

> Lấy `Host`, `Username`, `Password` từ connection string Neon ở Bước 2.

### Bước 5 — Cài dependencies và chạy

**Terminal 1 — Node.js backend:**

```bash
npm install
npm start
```

**Terminal 2 — .NET API:**

```bash
cd SyncChain.API
dotnet run
```

**Terminal 3 — MAUI Desktop:**

```bash
cd app/SyncChain.Desktop
dotnet run
```

Hoặc mở file `.sln` bằng **Visual Studio 2022** và nhấn F5.

---

## Lưu ý

- File `database/TaoBang.sql` là backup schema — **không cần chạy**, DB đã có sẵn trên Neon.
- File `.env` bị gitignore — mỗi người phải tự tạo từ `.env.example`.
- `appsettings.json` chứa placeholder, **phải điền connection string Neon thật** theo Bước 4 trước khi chạy.
- Lệnh `git update-index --skip-worktree` giúp git không track thay đổi local của `appsettings.json`, tránh vô tình commit thông tin kết nối lên repo.
