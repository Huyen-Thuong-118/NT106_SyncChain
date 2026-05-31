const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { pool } = require('./db');

// Khởi tạo app Express
const app = express();
app.use(cors());
app.use(express.json());

// Khởi tạo cấu trúc bảng (nếu chưa có) từ file schema PostgreSQL
async function initDatabase() {
  const schemaPath = path.resolve(__dirname, '../database/TaoBang.sql');
  const sql = fs.readFileSync(schemaPath, 'utf8');
  await pool.query(sql);
  console.log('✅ Đã khởi tạo bảng (nếu chưa có).');
}

// API GET /api/sanpham: trả về danh sách tất cả sản phẩm
// Lưu ý: PostgreSQL hạ tên cột không có nháy kép thành chữ thường,
// nên dùng alias "..." để giữ nguyên tên trường gửi về frontend.
app.get('/api/sanpham', async (req, res) => {
  const sql = `
    SELECT MaSanPham   AS "MaSanPham",
           TenSanPham  AS "TenSanPham",
           GiaBan      AS "GiaBan",
           SoLuongTon  AS "SoLuongTon",
           MucTonThap  AS "MucTonThap",
           TrangThai   AS "TrangThai"
    FROM SanPham
  `;
  try {
    const result = await pool.query(sql);
    res.json({ success: true, data: result.rows });
  } catch (err) {
    console.error('Lỗi truy vấn SanPham:', err.message);
    res.status(500).json({
      success: false,
      message: 'Lỗi truy vấn dữ liệu sản phẩm',
    });
  }
});

// Khởi động server lắng nghe PORT 3000
const PORT = 3000;
initDatabase()
  .then(() => {
    app.listen(PORT, () => {
      console.log(`🚀 SyncChain API đã khởi động - nhịp đập hệ thống tại http://localhost:3000`);
    });
  })
  .catch((err) => {
    console.error('Lỗi kết nối/khởi tạo PostgreSQL:', err.message);
    process.exit(1);
  });
