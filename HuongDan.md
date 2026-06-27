# NT106_SyncChain

SyncChain là ứng dụng quản lý bán hàng và tồn kho gồm:

- Backend API viết bằng ASP.NET Core.
- Ứng dụng desktop viết bằng .NET MAUI.
- Cơ sở dữ liệu PostgreSQL, cấu hình bằng biến `DATABASE_URL` trong file `.env`.
- Swagger/Postman để kiểm thử API.

## Chức năng chính

- Đăng ký, đăng nhập và xác thực bằng JWT.
- Phân quyền người dùng theo vai trò: `customer`, `staff`, `manager`, `admin`.
- Quản lý sản phẩm, giá bán, giá nhập, hình ảnh, tồn kho và trạng thái bán hàng.
- Tạo đơn hàng, xem chi tiết đơn hàng và cập nhật trạng thái xử lý.
- Nhập kho và xem lịch sử giao dịch kho.
- Dashboard báo cáo doanh thu, đơn hàng, sản phẩm bán chạy và cảnh báo tồn kho thấp.
- Quản trị tài khoản nội bộ dành cho admin.

## Yêu cầu môi trường

- Windows 10 trở lên.
- .NET SDK có hỗ trợ:
  - `net10.0` cho `SyncChain.API`.
  - `net9.0-windows10.0.19041.0` và workload MAUI cho `SyncChain.Desktop`.
- Visual Studio 2022 hoặc JetBrains Rider/VS Code có hỗ trợ .NET MAUI nếu muốn chạy app desktop bằng IDE.
- Node.js chỉ cần nếu muốn chạy thử phần script mẫu trong `src`.
- PostgreSQL đang chạy và đã có database `syncchain`.

Cài MAUI workload nếu máy chưa có:

```powershell
dotnet workload install maui
```

## Cách chạy dự án

### 1. Clone hoặc mở thư mục dự án

```powershell
cd NT106_SyncChain
```

### 2. Cấu hình PostgreSQL

Sao chép file cấu hình mẫu:

```powershell
Copy-Item .env.example .env
```

Cập nhật tài khoản, mật khẩu, host, port và tên database trong `.env`:

```text
DATABASE_URL=postgresql://postgres:matkhau@localhost:5432/syncchain
```

### 3. Restore package .NET

```powershell
dotnet restore NT106_SyncChain.sln
```

### 4. Chạy Backend API

```powershell
dotnet run --project SyncChain.API\SyncChain.API.csproj
```

API mặc định chạy tại:

- `http://localhost:5292`
- Swagger: `http://localhost:5292/swagger`

Khi API khởi động, Entity Framework Core kết nối PostgreSQL, tạo schema hiện tại nếu database đang trống và seed các vai trò mặc định.

Tài khoản admin mặc định:

```text
Email: admin@gmail.com
Password: 123456
```

### 5. Chạy ứng dụng Desktop

Mở terminal khác và chạy:

```powershell
dotnet run --project app\SyncChain.Desktop\SyncChain.Desktop.csproj
```

Lưu ý: app desktop đang gọi API tại `http://localhost:5292/`, vì vậy cần chạy API trước khi đăng nhập hoặc sử dụng dữ liệu thật.

## Cấu trúc thư mục

```text
NT106_SyncChain/
├── app/
│   └── SyncChain.Desktop/
├── database/
├── postman/
├── .postman/
├── src/
├── SyncChain.API/
├── ui/
├── NT106_SyncChain.sln
├── package.json
├── package-lock.json
├── index.json
├── LICENSE
└── README.md
```
