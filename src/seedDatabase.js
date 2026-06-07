// src/seedDatabase.js
// Nạp dữ liệu mẫu vào PostgreSQL, khớp với schema trong database/TaoBang.sql.
// Chạy: node src/seedDatabase.js
const fs = require('fs');
const path = require('path');
const { pool } = require('./db');

const SQL_SCHEMA_FILE = path.resolve(__dirname, '../database/TaoBang.sql');

async function seed() {
  const client = await pool.connect();
  try {
    console.log('Bắt đầu seed dữ liệu PostgreSQL...');

    // Đảm bảo các bảng đã tồn tại
    const schema = fs.readFileSync(SQL_SCHEMA_FILE, 'utf8');
    await client.query(schema);

    await client.query('BEGIN');

    // --- Vai trò (PhanQuyen) ---
    // ON CONFLICT để chạy lại nhiều lần không bị lỗi trùng (idempotent).
    // MaVaiTro cố định khớp backend .NET (AuthService): 1=customer, 2=staff, 3=manager, 4=admin.
    const vaiTro = [
      [1, 'customer'],
      [2, 'staff'],
      [3, 'manager'],
      [4, 'admin'],
    ];
    for (const [ma, ten] of vaiTro) {
      await client.query(
        `INSERT INTO PhanQuyen (MaVaiTro, TenVaiTro) VALUES ($1, $2)
         ON CONFLICT (MaVaiTro) DO UPDATE SET TenVaiTro = EXCLUDED.TenVaiTro`,
        [ma, ten]
      );
    }
    // Đẩy sequence vượt qua các ID đã insert thủ công để tránh conflict PK sau này.
    await client.query(
      `SELECT setval(pg_get_serial_sequence('PhanQuyen', 'mavaitro'),
                     GREATEST((SELECT MAX(MaVaiTro) FROM PhanQuyen), 1))`
    );

    // --- Người dùng (NguoiDung) ---
    // MatKhauHash ở đây chỉ là chuỗi giả lập cho dữ liệu mẫu.
    const nguoiDung = [
      ['admin',    'hash_admin',    'admin@syncchain.vn',    'admin'],
      ['nvkho01',  'hash_nvkho',    'kho@syncchain.vn',      'staff'],
      ['quanly01', 'hash_quanly',   'manager@syncchain.vn',  'manager'],
      ['khachA',   'hash_khach',    'customer@gmail.com',    'customer'],
    ];
    for (const [tenDangNhap, matKhauHash, email, tenVaiTro] of nguoiDung) {
      await client.query(
        `INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, Email, MaVaiTro)
         VALUES ($1, $2, $3, (SELECT MaVaiTro FROM PhanQuyen WHERE TenVaiTro = $4))
         ON CONFLICT (TenDangNhap) DO NOTHING`,
        [tenDangNhap, matKhauHash, email, tenVaiTro]
      );
    }

    // --- Sản phẩm (SanPham) ---
    // TenSanPham không có ràng buộc UNIQUE nên chỉ seed khi bảng còn rỗng,
    // tránh nhân đôi dữ liệu khi chạy lại script.
    const { rows: spCount } = await client.query('SELECT COUNT(*)::int AS n FROM SanPham');
    if (spCount[0].n === 0) {
      const sanPham = [
        ['Samsung Galaxy S24',     20000000, 100, 10],
        ['Bàn phím cơ Logitech',     500000,  50, 10],
        ['Chuột không dây',          300000,   8, 10], // dưới mức tồn thấp để test cảnh báo
      ];
      for (const [ten, gia, ton, mucThap] of sanPham) {
        await client.query(
          `INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon, MucTonThap)
           VALUES ($1, $2, $3, $4)`,
          [ten, gia, ton, mucThap]
        );
      }
    }

    await client.query('COMMIT');
    console.log('Seed hoàn tất!');
  } catch (err) {
    await client.query('ROLLBACK');
    console.error('Lỗi khi seed:', err.message);
    process.exitCode = 1;
  } finally {
    client.release();
    await pool.end();
  }
}

seed();
