// src/seedDatabase.js
// Nạp dữ liệu mẫu vào PostgreSQL, khớp với schema trong database/TaoBang.sql.
// Chạy: node src/seedDatabase.js
const fs = require('fs');
const path = require('path');
const bcrypt = require('bcryptjs');
const { pool } = require('./db');

// Mật khẩu mặc định cho mọi tài khoản seed (dùng để test đăng nhập).
const MAT_KHAU_MAC_DINH = '123456';

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
    const vaiTro = ['Admin', 'NhanVienKho', 'NhaPhanPhoi', 'KhachHang'];
    for (const ten of vaiTro) {
      await client.query(
        'INSERT INTO PhanQuyen (TenVaiTro) VALUES ($1) ON CONFLICT (TenVaiTro) DO NOTHING',
        [ten]
      );
    }

    // --- Người dùng (NguoiDung) ---
    // Mật khẩu băm bằng bcrypt; DO UPDATE để cập nhật hash cho tài khoản đã seed trước đó.
    const matKhauHash = await bcrypt.hash(MAT_KHAU_MAC_DINH, 10);
    const nguoiDung = [
      ['admin',    'admin@syncchain.vn',  'Admin'],
      ['nvkho01',  'kho@syncchain.vn',    'NhanVienKho'],
      ['nppabc',   'supplier@abc.com',    'NhaPhanPhoi'],
      ['khachA',   'customer@gmail.com',  'KhachHang'],
    ];
    for (const [tenDangNhap, email, tenVaiTro] of nguoiDung) {
      await client.query(
        `INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, Email, MaVaiTro)
         VALUES ($1, $2, $3, (SELECT MaVaiTro FROM PhanQuyen WHERE TenVaiTro = $4))
         ON CONFLICT (TenDangNhap) DO UPDATE SET MatKhauHash = EXCLUDED.MatKhauHash`,
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
    console.log(`👤 Tài khoản seed: admin / nvkho01 / nppabc / khachA — mật khẩu: ${MAT_KHAU_MAC_DINH}`);
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
