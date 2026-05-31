// src/server.js
// Điểm khởi động: tạo app Express, gắn middleware + routes, khởi tạo DB rồi lắng nghe.
const express = require('express');
const cors = require('cors');
const { initDatabase } = require('./db');
const authRoutes = require('./routes/authRoutes');
const sanPhamRoutes = require('./routes/sanPhamRoutes');
const donHangRoutes = require('./routes/donHangRoutes');

const app = express();
app.use(cors());
app.use(express.json());

// Gắn các nhóm route
app.use('/api/auth', authRoutes);
app.use('/api/sanpham', sanPhamRoutes);
app.use('/api/donhang', donHangRoutes);

// Khởi động server lắng nghe PORT 3000
const PORT = 3000;
initDatabase()
  .then(() => {
    app.listen(PORT, () => {
      console.log(`🚀 SyncChain API đã khởi động - nhịp đập hệ thống tại http://localhost:${PORT}`);
    });
  })
  .catch((err) => {
    console.error('Lỗi kết nối/khởi tạo PostgreSQL:', err.message);
    process.exit(1);
  });

module.exports = app;
