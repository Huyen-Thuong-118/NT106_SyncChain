// src/models/sanPhamModel.js
// Tầng truy vấn dữ liệu cho bảng SanPham.
const { pool } = require('../db');

// Alias "..." để giữ nguyên hoa/thường tên cột (Postgres tự hạ chữ thường).
const COLS = `
  MaSanPham  AS "MaSanPham",
  TenSanPham AS "TenSanPham",
  GiaBan     AS "GiaBan",
  SoLuongTon AS "SoLuongTon",
  MucTonThap AS "MucTonThap",
  TrangThai  AS "TrangThai"
`;

async function layTatCa() {
  const { rows } = await pool.query(`SELECT ${COLS} FROM SanPham ORDER BY MaSanPham`);
  return rows;
}

async function them({ TenSanPham, GiaBan, SoLuongTon, MucTonThap }) {
  const { rows } = await pool.query(
    `INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon, MucTonThap)
     VALUES ($1, $2, $3, COALESCE($4, 10))
     RETURNING ${COLS}`,
    [TenSanPham, GiaBan, SoLuongTon, MucTonThap]
  );
  return rows[0];
}

module.exports = { layTatCa, them };
