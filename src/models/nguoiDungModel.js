// src/models/nguoiDungModel.js
// Tầng truy vấn dữ liệu cho bảng NguoiDung.
const { pool } = require('../db');

// Tìm theo tên đăng nhập, kèm tên vai trò (để phân quyền). Trả null nếu không có.
async function timTheoTenDangNhap(tenDangNhap) {
  const { rows } = await pool.query(
    `SELECT u.MaNguoiDung AS "MaNguoiDung",
            u.TenDangNhap AS "TenDangNhap",
            u.MatKhauHash AS "MatKhauHash",
            u.Email       AS "Email",
            pq.TenVaiTro  AS "VaiTro"
     FROM NguoiDung u
     LEFT JOIN PhanQuyen pq ON pq.MaVaiTro = u.MaVaiTro
     WHERE u.TenDangNhap = $1`,
    [tenDangNhap]
  );
  return rows[0] || null;
}

// Tạo người dùng mới với vai trò theo tên (vd 'KhachHang').
async function taoNguoiDung({ TenDangNhap, MatKhauHash, Email, tenVaiTro }) {
  const { rows } = await pool.query(
    `INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, Email, MaVaiTro)
     VALUES ($1, $2, $3, (SELECT MaVaiTro FROM PhanQuyen WHERE TenVaiTro = $4))
     RETURNING MaNguoiDung AS "MaNguoiDung", TenDangNhap AS "TenDangNhap", Email AS "Email"`,
    [TenDangNhap, MatKhauHash, Email, tenVaiTro]
  );
  return rows[0];
}

async function capNhatLanDangNhap(maNguoiDung) {
  await pool.query(
    'UPDATE NguoiDung SET LanDangNhapCuoi = CURRENT_TIMESTAMP WHERE MaNguoiDung = $1',
    [maNguoiDung]
  );
}

module.exports = { timTheoTenDangNhap, taoNguoiDung, capNhatLanDangNhap };
