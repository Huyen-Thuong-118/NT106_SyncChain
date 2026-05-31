// src/index.js
// Script demo/thử nghiệm cho tầng dữ liệu PostgreSQL.
// Tạo bảng (nếu chưa có) rồi chạy vài thao tác mẫu: thêm sản phẩm, đặt hàng.
// Chạy trực tiếp: node src/index.js
// Lưu ý: API thật nằm ở src/server.js — file này chỉ để thử logic dưới DB.
const fs = require('fs');
const path = require('path');
const { pool } = require('./db');
const { ORDER_STATUS } = require('./constants/statusEnum');

const SQL_SCHEMA_FILE = path.resolve(__dirname, '../database/TaoBang.sql');

async function initDatabase() {
  const sql = fs.readFileSync(SQL_SCHEMA_FILE, 'utf8');
  await pool.query(sql);
  console.log('✅ Đã khởi tạo bảng (nếu chưa có).');
}

// Thêm sản phẩm mới (mô phỏng nhà phân phối nhập hàng).
// Dùng $1, $2... để chống SQL injection.
async function themSanPham(ten, gia, soLuong) {
  const sql = `INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon)
               VALUES ($1, $2, $3) RETURNING MaSanPham`;
  const result = await pool.query(sql, [ten, gia, soLuong]);
  const maSP = result.rows[0].masanpham;
  console.log(`✅ Đã thêm sản phẩm [${ten}] - Mã SP: ${maSP} - Tồn: ${soLuong}`);
  return maSP;
}

// Đặt hàng an toàn (transaction): trừ tồn kho có điều kiện để không "bán lố"
// khi nhiều người mua cùng lúc, và lấy GiaBan từ DB (không tin giá client gửi).
async function datHang(maKhachHang, maSanPham, soLuong) {
  console.log(`\n⏳ Khách ${maKhachHang} đặt mua SP ${maSanPham} (x${soLuong})...`);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');

    // Chỉ trừ khi đủ tồn; RETURNING để lấy luôn đơn giá từ DB.
    const upd = await client.query(
      `UPDATE SanPham
          SET SoLuongTon = SoLuongTon - $1
        WHERE MaSanPham = $2 AND SoLuongTon >= $1
        RETURNING GiaBan`,
      [soLuong, maSanPham]
    );
    if (upd.rowCount === 0) {
      await client.query('ROLLBACK');
      console.log('⚠️ Thất bại: kho không đủ hàng hoặc sản phẩm không tồn tại.');
      return;
    }

    const donGia = Number(upd.rows[0].giaban);
    const tongTien = donGia * soLuong;

    // Trạng thái khởi tạo lấy từ statusEnum.js (Draft).
    const insDon = await client.query(
      `INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon)
       VALUES ($1, $2, $3) RETURNING MaDonHang`,
      [maKhachHang, tongTien, ORDER_STATUS.DRAFT]
    );
    const maDonHang = insDon.rows[0].madonhang;

    await client.query(
      `INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia)
       VALUES ($1, $2, $3, $4)`,
      [maDonHang, maSanPham, soLuong, donGia]
    );

    await client.query('COMMIT');
    console.log(`🎉 Đã chốt đơn #${maDonHang} - Tổng: ${tongTien}. Tồn kho trừ an toàn!`);
    return { maDonHang, tongTien };
  } catch (err) {
    await client.query('ROLLBACK');
    console.error('❌ Lỗi:', err.message);
    throw err;
  } finally {
    client.release();
  }
}

// Khu vực chạy thử nghiệm.
async function chayThuNghiem() {
  await initDatabase();
  const maSP = await themSanPham('Bàn phím cơ Logitech', 500000, 10);
  await datHang(1, maSP, 2);    // đơn hợp lệ
  await datHang(2, maSP, 100);  // cố tình đặt lố -> phải bị chặn
  await pool.end();
}

module.exports = { pool, themSanPham, datHang };

// Chỉ chạy thử nghiệm khi gọi trực tiếp `node src/index.js`.
if (require.main === module) {
  chayThuNghiem().catch((err) => {
    console.error(err);
    process.exit(1);
  });
}
