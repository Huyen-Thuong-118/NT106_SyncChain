// src/db.js
// Pool kết nối PostgreSQL dùng chung cho toàn bộ ứng dụng.
require('dotenv').config();
const { Pool } = require('pg');

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
});

pool.on('error', (err) => {
  console.error('Lỗi không mong muốn từ client PostgreSQL nhàn rỗi:', err);
});

module.exports = { pool };
