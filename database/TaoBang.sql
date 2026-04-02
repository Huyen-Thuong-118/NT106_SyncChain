-- 1. Phân Quyền
CREATE TABLE IF NOT EXISTS PhanQuyen (
    MaVaiTro INTEGER PRIMARY KEY AUTOINCREMENT,
    TenVaiTro TEXT NOT NULL UNIQUE
);

-- 2. Người Dùng
CREATE TABLE IF NOT EXISTS NguoiDung (
    MaNguoiDung INTEGER PRIMARY KEY AUTOINCREMENT,
    TenDangNhap TEXT UNIQUE NOT NULL,
    MatKhauHash TEXT NOT NULL, 
    Email TEXT UNIQUE NOT NULL,
    MaVaiTro INTEGER,
    NgayTao DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaVaiTro) REFERENCES PhanQuyen(MaVaiTro)
);

-- 3. Sản Phẩm & Tồn Kho
CREATE TABLE IF NOT EXISTS SanPham (
    MaSanPham INTEGER PRIMARY KEY AUTOINCREMENT,
    TenSanPham TEXT NOT NULL,
    GiaBan REAL NOT NULL,
    SoLuongTon INTEGER NOT NULL CHECK (SoLuongTon >= 0),
    MucTonThap INTEGER DEFAULT 10,
    TrangThai TEXT DEFAULT 'Hoat dong'
);

-- 4. Đơn Hàng Chính
CREATE TABLE IF NOT EXISTS DonHang (
    MaDonHang INTEGER PRIMARY KEY AUTOINCREMENT,
    MaKhachHang INTEGER,
    TongTien REAL NOT NULL,
    TrangThaiDon TEXT DEFAULT 'Da dat hang', 
    NgayTao DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaKhachHang) REFERENCES NguoiDung(MaNguoiDung)
);

-- 5. Chi Tiết Đơn Hàng
CREATE TABLE IF NOT EXISTS ChiTietDonHang (
    MaChiTiet INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDonHang INTEGER,
    MaSanPham INTEGER,
    SoLuong INTEGER NOT NULL CHECK (SoLuong > 0),
    DonGia REAL NOT NULL,
    FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);

-- 6. Đơn Nhập Hàng
CREATE TABLE IF NOT EXISTS DonNhapHang (
    MaDonNhap INTEGER PRIMARY KEY AUTOINCREMENT,
    MaNhaPhanPhoi INTEGER,
    TrangThaiDuyet TEXT DEFAULT 'Cho duyet', 
    NgayTao DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaNhaPhanPhoi) REFERENCES NguoiDung(MaNguoiDung)
);

-- 7. Chi Tiết Đơn Nhập
CREATE TABLE IF NOT EXISTS ChiTietDonNhap (
    MaChiTietNhap INTEGER PRIMARY KEY AUTOINCREMENT,
    MaDonNhap INTEGER,
    MaSanPham INTEGER,
    SoLuongYeuCau INTEGER NOT NULL CHECK (SoLuongYeuCau > 0),
    FOREIGN KEY (MaDonNhap) REFERENCES DonNhapHang(MaDonNhap),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);

SELECT name FROM sqlite_master WHERE type='table';
SELECT * FROM SanPham;
