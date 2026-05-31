// src/db.js
// Pool kết nối PostgreSQL dùng chung cho toàn bộ ứng dụng + hàm khởi tạo schema.
require('dotenv').config();
const fs = require('fs');
const path = require('path');
const { Pool } = require('pg');

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
});

pool.on('error', (err) => {
  console.error('Lỗi không mong muốn từ client PostgreSQL nhàn rỗi:', err);
});

// Tạo bảng (nếu chưa có) và áp các migration từ file schema PostgreSQL.
async function initDatabase() {
  const schemaPath = path.resolve(__dirname, '../database/TaoBang.sql');
  const sql = fs.readFileSync(schemaPath, 'utf8');
  await pool.query(sql);
  console.log('✅ Đã khởi tạo bảng (nếu chưa có).');
}

module.exports = { pool, initDatabase };
