# RetailOps - Hệ thống quản lý vận hành chuỗi bán lẻ dựa trên mô hình Client-Server

## 📖 Giới thiệu
Đây là đồ án môn Lập trình mạng căn bản. Ứng dụng cung cấp giải pháp [Mô tả ngắn gọn trong 1-2 câu: ví dụ: chat đa luồng, truyền nhận file qua mạng LAN, hoặc ứng dụng quản lý từ xa...].

## ✨ Chức năng chính
* **Giao diện:** Thiết kế thân thiện, trực quan bằng Windows Forms.
* **Server:** * Khởi tạo và lắng nghe kết nối từ nhiều Client cùng lúc.
  * Xử lý đa luồng (Multi-threading).
  * Ghi log hoạt động và lưu trữ dữ liệu (I/O Stream).
* **Client:** * Kết nối an toàn tới Server thông qua IP và Port.
  * [Chức năng A của Client]
  * [Chức năng B của Client]

## 🛠 Công nghệ và Thư viện sử dụng
* **Ngôn ngữ lập trình:** C#
* **Nền tảng:** .NET Framework (Windows Forms)
* **Giao thức mạng:** TCP/IP (hoặc UDP) thông qua thư viện `System.Net.Sockets`.
* **Khác:** Xử lý luồng (`System.Threading`), Xử lý file (`System.IO`).

## 🚀 Hướng dẫn Cài đặt và Sử dụng

### Yêu cầu hệ thống
* Máy tính cài đặt sẵn Visual Studio.
* Hai máy tính cùng kết nối chung một mạng LAN (nếu muốn test thực tế), hoặc test trực tiếp trên cùng một máy (Localhost).

### Các bước chạy chương trình
1. Tải toàn bộ mã nguồn về máy và giải nén.
2. Mở file Solution (`.sln`) bằng Visual Studio.
3. Nhấn chuột phải vào Solution -> Chọn **Build Solution** để khôi phục các thư viện cần thiết.
4. **Khởi động Server:**
   * Mở project Server và nhấn Start.
   * Cấp quyền truy cập mạng nếu Windows Firewall xuất hiện hộp thoại cảnh báo (Chọn *Allow access*).
   * Nhấn nút "Khởi động Server" trên giao diện. Server sẽ lắng nghe ở Port mặc định (ví dụ: `8080`).
5. **Khởi động Client:**
   * Mở project Client và nhấn Start.
   * Nhập địa chỉ IP của Server (Nhập `127.0.0.1` nếu chạy trên cùng 1 máy) và Port tương ứng.
   * Nhấn "Kết nối" và bắt đầu sử dụng.

6. **Cây thư mục**
   NT106_SyncChain/
   ├── server/                 # Toàn bộ logic Node.js hiện tại chuyển vào đây
   │   ├── src/
   │   │   ├── controllers/    # Xử lý logic API
   │   │   ├── routes/         # Định nghĩa các endpoint
   │   │   ├── models/         # Định nghĩa Schema/Query
   │   │   └── server.js
   │   ├── database/           # Chứa file .db và các file Migration/Seed
   │   └── package.json
   ├── client/                 # Tạo mới - Project C# WinForms
   │   ├── RetailOps.sln
   │   ├── Models/             # Map dữ liệu JSON từ API về Class C#
   │   └── Services/           # Nơi chứa code HttpClient gọi lên Server
   ├── docs/                   # Tài liệu đồ án, sơ đồ Sequence, Class Diagram
   ├── .gitignore              # Cập nhật để chặn cả bin/obj của C# và node_modules
   └── README.md


## 👥 Thành viên thực hiện
* **Nguyễn Đỗ Ngọc Huyền Thương** - 24521750 - [Vai trò: Code Server/Code UI...]
* **Tăng Thanh Thư** - 245217xx - [Vai trò: Code Client/Viết báo cáo...]
* **Mai Lương Khánh Vy** - 24521xxx - [Vai trò: Code Client/Viết báo cáo...]
