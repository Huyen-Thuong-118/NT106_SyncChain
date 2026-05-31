const express = require('express');
const cors = require('cors');
const sqlite3 = require('sqlite3').verbose();
const path = require('path');

// Khởi tạo app Express
const app = express();
app.use(cors());
app.use(express.json());

// Kết nối SQLite đến file database/SyncChain.db
const dbPath = path.resolve(__dirname, '../database/SyncChain.db');
const db = new sqlite3.Database(dbPath, sqlite3.OPEN_READWRITE, (err) => {
  if (err) {
    console.error('Lỗi kết nối SQLite:', err.message);
    process.exit(1);
  }
  console.log('✅ Đã kết nối SQLite:', dbPath);
});

// API GET /api/sanpham: trả về danh sách tất cả sản phẩm
app.get('/api/sanpham', (req, res) => {
  const sql = `
    SELECT MaSanPham,
           TenSanPham,
           GiaBan,
           SoLuongTon,
           MucTonThap,
           TrangThai
    FROM SanPham
  `;
  db.all(sql, [], (err, rows) => {
    if (err) {
      console.error('Lỗi truy vấn SanPham:', err.message);
      return res.status(500).json({
        success: false,
        message: 'Lỗi truy vấn dữ liệu sản phẩm'
      });
    }
    res.json({ success: true, data: rows });
  });
});

// Khởi động server lắng nghe PORT 3000
const PORT = 3000;
app.listen(PORT, () => {
  console.log(`🚀 SyncChain API đã khởi động - nhịp đập hệ thống tại http://localhost:${PORT}`);
});