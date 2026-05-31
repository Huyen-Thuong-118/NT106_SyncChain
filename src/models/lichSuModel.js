// src/models/lichSuModel.js
// Ghi nhật ký hoạt động / đăng nhập vào bảng LichSuHoatDong.
const { pool } = require('../db');

async function ghi(maNguoiDung, hanhDong, moTa, diaChiIP) {
  await pool.query(
    `INSERT INTO LichSuHoatDong (MaNguoiDung, HanhDong, MoTa, DiaChiIP)
     VALUES ($1, $2, $3, $4)`,
    [maNguoiDung, hanhDong, moTa, diaChiIP]
  );
}

module.exports = { ghi };
