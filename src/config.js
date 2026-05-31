// src/config.js
// Cấu hình dùng chung. JWT_SECRET nên đặt trong .env ở môi trường thật.
require('dotenv').config();

module.exports = {
  JWT_SECRET: process.env.JWT_SECRET || 'dev_secret_doi_trong_production',
  JWT_EXPIRES: '8h',
};
