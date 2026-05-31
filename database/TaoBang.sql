-- 1. Phân Quyền
CREATE TABLE IF NOT EXISTS PhanQuyen (
    MaVaiTro SERIAL PRIMARY KEY,
    TenVaiTro TEXT NOT NULL UNIQUE
);

-- 2. Người Dùng
CREATE TABLE IF NOT EXISTS NguoiDung (
    MaNguoiDung SERIAL PRIMARY KEY,
    TenDangNhap TEXT UNIQUE NOT NULL,
    MatKhauHash TEXT NOT NULL,
    Email TEXT UNIQUE NOT NULL,
    MaVaiTro INTEGER,
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaVaiTro) REFERENCES PhanQuyen(MaVaiTro)
);

-- 3. Sản Phẩm & Tồn Kho
CREATE TABLE IF NOT EXISTS SanPham (
    MaSanPham SERIAL PRIMARY KEY,
    TenSanPham TEXT NOT NULL,
    GiaBan NUMERIC(15,2) NOT NULL,
    SoLuongTon INTEGER NOT NULL CHECK (SoLuongTon >= 0),
    MucTonThap INTEGER DEFAULT 10,
    TrangThai TEXT DEFAULT 'Hoat dong'
);

-- 4. Đơn Hàng Chính
CREATE TABLE IF NOT EXISTS DonHang (
    MaDonHang SERIAL PRIMARY KEY,
    MaKhachHang INTEGER,
    TongTien NUMERIC(15,2) NOT NULL,
    TrangThaiDon TEXT DEFAULT 'Da dat hang',
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaKhachHang) REFERENCES NguoiDung(MaNguoiDung)
);

-- 5. Chi Tiết Đơn Hàng
CREATE TABLE IF NOT EXISTS ChiTietDonHang (
    MaChiTiet SERIAL PRIMARY KEY,
    MaDonHang INTEGER,
    MaSanPham INTEGER,
    SoLuong INTEGER NOT NULL CHECK (SoLuong > 0),
    DonGia NUMERIC(15,2) NOT NULL,
    FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);

-- 6. Đơn Nhập Hàng
CREATE TABLE IF NOT EXISTS DonNhapHang (
    MaDonNhap SERIAL PRIMARY KEY,
    MaNhaPhanPhoi INTEGER,
    TrangThaiDuyet TEXT DEFAULT 'Cho duyet',
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaNhaPhanPhoi) REFERENCES NguoiDung(MaNguoiDung)
);

-- 7. Chi Tiết Đơn Nhập
CREATE TABLE IF NOT EXISTS ChiTietDonNhap (
    MaChiTietNhap SERIAL PRIMARY KEY,
    MaDonNhap INTEGER,
    MaSanPham INTEGER,
    SoLuongYeuCau INTEGER NOT NULL CHECK (SoLuongYeuCau > 0),
    FOREIGN KEY (MaDonNhap) REFERENCES DonNhapHang(MaDonNhap),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);
