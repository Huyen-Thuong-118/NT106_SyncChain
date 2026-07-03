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
    MaDonHang INTEGER NOT NULL,
    MaSanPham INTEGER NOT NULL,
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
    MaDonNhap INTEGER NOT NULL,
    MaSanPham INTEGER NOT NULL,
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

-- ════════════════════════════════════════════════════════════════
-- Migration idempotent: bổ sung cột mới vào bảng đã tồn tại.
-- CREATE TABLE IF NOT EXISTS bỏ qua bảng cũ nên cần ALTER riêng.
-- ════════════════════════════════════════════════════════════════

-- NguoiDung
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS DaXacThucEmail BOOLEAN DEFAULT FALSE;
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS MaXacThuc TEXT;
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS LanDangNhapCuoi TIMESTAMP;
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS KichHoat BOOLEAN NOT NULL DEFAULT TRUE;

-- SanPham
ALTER TABLE SanPham ADD COLUMN IF NOT EXISTS GiaNhap NUMERIC(15,2) NOT NULL DEFAULT 0;
ALTER TABLE SanPham ADD COLUMN IF NOT EXISTS HinhAnhUrl TEXT NOT NULL DEFAULT '';
ALTER TABLE SanPham ADD COLUMN IF NOT EXISTS MoTa TEXT NOT NULL DEFAULT '';

-- DonHang
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS PhuongThucThanhToan TEXT DEFAULT 'COD';
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS NguoiNhan TEXT;
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS SoDienThoaiNhan TEXT;
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS DiaChiGiao TEXT;
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS MaVanDon TEXT;
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS DonViVanChuyen TEXT;
ALTER TABLE DonHang ADD COLUMN IF NOT EXISTS NgayCapNhat TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- Fix 5: FK columns phải NOT NULL (bảng đã tồn tại dùng ALTER)
-- Lệnh này sẽ lỗi nếu có dữ liệu NULL trong cột — kiểm tra trước khi chạy.
ALTER TABLE ChiTietDonHang ALTER COLUMN MaDonHang SET NOT NULL;
ALTER TABLE ChiTietDonHang ALTER COLUMN MaSanPham SET NOT NULL;
ALTER TABLE ChiTietDonNhap ALTER COLUMN MaDonNhap SET NOT NULL;
ALTER TABLE ChiTietDonNhap ALTER COLUMN MaSanPham SET NOT NULL;

-- Fix 6: Trigger tự cập nhật NgayCapNhat khi UPDATE DonHang
CREATE OR REPLACE FUNCTION fn_set_ngaycapnhat()
RETURNS TRIGGER AS $$
BEGIN
    NEW.NgayCapNhat = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_donhang_ngaycapnhat ON DonHang;
CREATE TRIGGER trg_donhang_ngaycapnhat
    BEFORE UPDATE ON DonHang
    FOR EACH ROW EXECUTE FUNCTION fn_set_ngaycapnhat();

-- Fix 7: Index cho các query phổ biến
CREATE INDEX IF NOT EXISTS idx_donhang_khachhang   ON DonHang(MaKhachHang);
CREATE INDEX IF NOT EXISTS idx_donhang_trangthai   ON DonHang(TrangThaiDon);
CREATE INDEX IF NOT EXISTS idx_chitiet_donhang     ON ChiTietDonHang(MaDonHang);
CREATE INDEX IF NOT EXISTS idx_chitiet_sanpham     ON ChiTietDonHang(MaSanPham);
CREATE INDEX IF NOT EXISTS idx_kho_sanpham         ON GiaoDichKho(MaSanPham);
CREATE INDEX IF NOT EXISTS idx_kho_loai_thoigian   ON GiaoDichKho(Loai, ThoiGian DESC);
CREATE INDEX IF NOT EXISTS idx_lichsu_nguoidung    ON LichSuHoatDong(MaNguoiDung);
CREATE INDEX IF NOT EXISTS idx_lichsu_thoigian     ON LichSuHoatDong(ThoiGian DESC);

-- ════════════════════════════════════════════════════════════════
-- Customer Account Module — schema mới
-- ════════════════════════════════════════════════════════════════

-- NguoiDung: bổ sung thông tin cá nhân
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS Ho TEXT;
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS Ten TEXT;
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS SoDienThoai TEXT;
ALTER TABLE NguoiDung ADD COLUMN IF NOT EXISTS NgayCapNhat TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- Unique constraint cho SoDienThoai (chạy idempotent)
DO $$ BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'nguoidung_sodienthoai_unique'
  ) THEN
    ALTER TABLE NguoiDung ADD CONSTRAINT nguoidung_sodienthoai_unique UNIQUE (SoDienThoai);
  END IF;
END $$;

-- 10. Địa Chỉ Giao Hàng
CREATE TABLE IF NOT EXISTS DiaChi (
    MaDiaChi SERIAL PRIMARY KEY,
    MaNguoiDung INTEGER NOT NULL,
    TenNguoiNhan TEXT NOT NULL,
    SoDienThoai TEXT NOT NULL,
    TinhThanh TEXT NOT NULL,
    QuanHuyen TEXT NOT NULL,
    PhuongXa TEXT NOT NULL,
    DiaChiChiTiet TEXT NOT NULL,
    LaMacDinh BOOLEAN NOT NULL DEFAULT FALSE,
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 11. OTP Quên Mật Khẩu
CREATE TABLE IF NOT EXISTS OtpReset (
    MaOtp SERIAL PRIMARY KEY,
    MaNguoiDung INTEGER NOT NULL,
    MaOtpHash TEXT NOT NULL,
    HetHan TIMESTAMP NOT NULL,
    DaDung BOOLEAN NOT NULL DEFAULT FALSE,
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 12. Giỏ Hàng (1 giỏ / khách)
CREATE TABLE IF NOT EXISTS GioHang (
    MaGioHang SERIAL PRIMARY KEY,
    MaNguoiDung INTEGER NOT NULL UNIQUE,
    NgayTao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    NgayCapNhat TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 13. Chi Tiết Giỏ Hàng
CREATE TABLE IF NOT EXISTS ChiTietGioHang (
    MaChiTietGio SERIAL PRIMARY KEY,
    MaGioHang INTEGER NOT NULL,
    MaSanPham INTEGER NOT NULL,
    SoLuong INTEGER NOT NULL CHECK (SoLuong > 0),
    NgayThem TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (MaGioHang, MaSanPham),
    FOREIGN KEY (MaGioHang) REFERENCES GioHang(MaGioHang),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);

-- Index hỗ trợ
CREATE INDEX IF NOT EXISTS idx_diachi_nguoidung  ON DiaChi(MaNguoiDung);
CREATE INDEX IF NOT EXISTS idx_otp_nguoidung     ON OtpReset(MaNguoiDung);
CREATE INDEX IF NOT EXISTS idx_giohang_nguoidung ON GioHang(MaNguoiDung);
CREATE INDEX IF NOT EXISTS idx_ctgio_giohang     ON ChiTietGioHang(MaGioHang);
