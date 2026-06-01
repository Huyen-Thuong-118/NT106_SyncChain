-- Schema PostgreSQL của SyncChain — khớp với DB thật (cập nhật 2026-06-01).
-- Dùng CREATE TABLE IF NOT EXISTS để chạy lại an toàn (idempotent).
-- Trạng thái lấy từ src/constants/statusEnum.js (nguồn chân lý duy nhất):
--   ORDER_STATUS, GRN_STATUS, INVENTORY_TXN_TYPE.

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
    DaXacThucEmail BOOLEAN DEFAULT FALSE,        -- đã xác thực email hay chưa
    MaXacThuc TEXT,                              -- mã xác thực email
    LanDangNhapCuoi TIMESTAMP,                   -- thời điểm đăng nhập gần nhất
    KichHoat BOOLEAN NOT NULL DEFAULT TRUE,      -- admin bật/tắt tài khoản
    FOREIGN KEY (MaVaiTro) REFERENCES PhanQuyen(MaVaiTro)
);

-- 3. Sản Phẩm & Tồn Kho
CREATE TABLE IF NOT EXISTS SanPham (
    MaSanPham SERIAL PRIMARY KEY,
    TenSanPham TEXT NOT NULL,
    GiaBan NUMERIC(15,2) NOT NULL,
    GiaNhap NUMERIC(15,2) NOT NULL DEFAULT 0,
    SoLuongTon INTEGER NOT NULL CHECK (SoLuongTon >= 0),
    MucTonThap INTEGER DEFAULT 10,
    TrangThai TEXT DEFAULT 'Hoat dong',
    HinhAnhUrl TEXT NOT NULL DEFAULT '',
    MoTa TEXT NOT NULL DEFAULT ''
);

-- 4. Đơn Hàng Chính
CREATE TABLE IF NOT EXISTS DonHang (
    MaDonHang SERIAL PRIMARY KEY,
    MaKhachHang INTEGER,
    TongTien NUMERIC(15,2) NOT NULL,
    -- Trạng thái đơn lấy từ statusEnum.js (ORDER_STATUS) - nguồn chân lý duy nhất.
    TrangThaiDon TEXT NOT NULL DEFAULT 'Draft'
        CHECK (TrangThaiDon IN ('Draft', 'Approved', 'Processing', 'Done', 'Cancelled')),
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Thông tin thanh toán & giao vận (COD).
    PhuongThucThanhToan TEXT DEFAULT 'COD',
    NguoiNhan TEXT,
    SoDienThoaiNhan TEXT,
    DiaChiGiao TEXT,
    MaVanDon TEXT,
    DonViVanChuyen TEXT,
    NgayCapNhat TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
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
    -- Trạng thái duyệt lấy từ statusEnum.js (GRN_STATUS) - nguồn chân lý duy nhất.
    TrangThaiDuyet TEXT NOT NULL DEFAULT 'Draft'
        CHECK (TrangThaiDuyet IN ('Draft', 'Approved', 'Done')),
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

-- 8. Giao Dịch Kho (sổ nhập/xuất/điều chỉnh tồn kho)
CREATE TABLE IF NOT EXISTS GiaoDichKho (
    MaGiaoDich SERIAL PRIMARY KEY,
    MaSanPham INTEGER NOT NULL,
    -- Loại giao dịch lấy từ statusEnum.js (INVENTORY_TXN_TYPE).
    Loai TEXT NOT NULL CHECK (Loai IN ('IN', 'OUT', 'ADJUST')),
    SoLuong INTEGER NOT NULL,
    ThoiGian TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    MaNguoiDung INTEGER,
    GhiChu TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham),
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 9. Lịch Sử Hoạt Động (nhật ký audit: đăng nhập, đổi trạng thái, thao tác kho...)
CREATE TABLE IF NOT EXISTS LichSuHoatDong (
    MaLichSu SERIAL PRIMARY KEY,
    MaNguoiDung INTEGER,
    HanhDong TEXT NOT NULL,
    MoTa TEXT,
    DiaChiIP TEXT,
    ThoiGian TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);
