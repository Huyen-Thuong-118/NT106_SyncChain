const fs = require('fs');
const path = require('path');
const { pool } = require('./db');

const SQL_SCHEMA_FILE = path.resolve(__dirname, '../database/TaoBang.sql');

async function initDatabase() {
  const sql = fs.readFileSync(SQL_SCHEMA_FILE, 'utf8');
  await pool.query(sql);
  console.log('✅ Đã khởi tạo bảng (nếu chưa có).');
}

/**
 * Hàm mẫu "Khách đặt hàng" (transaction bảo toàn).
 */
async function datDonHang(maKhachHang, maSanPham, soLuong) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');

    const { rows } = await client.query(
      'SELECT SoLuongTon, GiaBan FROM SanPham WHERE MaSanPham = $1',
      [maSanPham]
    );
    const row = rows[0];
    if (!row) {
      throw new Error('Sản phẩm không tồn tại.');
    }
    if (row.soluongton < soLuong) {
      throw new Error('Tồn kho không đủ.');
    }

    const donGia = row.giaban;
    const tongTien = donGia * soLuong;

    // Giảm tồn kho
    await client.query(
      'UPDATE SanPham SET SoLuongTon = SoLuongTon - $1 WHERE MaSanPham = $2',
      [soLuong, maSanPham]
    );

    // Tạo đơn hàng (RETURNING để lấy ID vừa tạo thay cho this.lastID)
    const insertDon = await client.query(
      'INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon) VALUES ($1, $2, $3) RETURNING MaDonHang',
      [maKhachHang, tongTien, 'DaDat']
    );
    const maDonHang = insertDon.rows[0].madonhang;

    await client.query(
      'INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia) VALUES ($1, $2, $3, $4)',
      [maDonHang, maSanPham, soLuong, donGia]
    );

    await client.query('COMMIT');
    return { maDonHang, tongTien };
  } catch (err) {
    await client.query('ROLLBACK');
    throw err;
  } finally {
    client.release();
  }
}

// Example test (uncomment để chạy trực tiếp)
// datDonHang(1, 1, 2).then(console.log).catch(console.error);

module.exports = { pool, datDonHang };

// ==========================================
// CÁC HÀM XỬ LÝ LOGIC CHO APP SYNCCHAIN
// ==========================================

// 1. Hàm Thêm Sản Phẩm (Mô phỏng Nhà phân phối nhập hàng mới lên app)
async function themSanPham(ten, gia, soLuong) {
  // Dùng $1, $2... để chống lỗi bảo mật SQL Injection
  const sql = `INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon) VALUES ($1, $2, $3) RETURNING MaSanPham`;
  try {
    const result = await pool.query(sql, [ten, gia, soLuong]);
    const maSP = result.rows[0].masanpham;
    console.log(`✅ Đã thêm sản phẩm: [${ten}] - Mã SP: ${maSP} - Tồn kho: ${soLuong}`);
    return maSP;
  } catch (err) {
    console.error('❌ Lỗi khi thêm sản phẩm:', err.message);
  }
}

// 2. Hàm Đặt Hàng An Toàn (Mô phỏng Khách hàng bấm mua)
// Hàm này dùng TRANSACTION để đảm bảo không bị lỗi "bán lố hàng" khi nhiều người mua cùng lúc
async function datHang(maKhachHang, maSanPham, soLuongMua, donGia) {
  console.log(`\n⏳ Đang xử lý đơn hàng cho Khách ID ${maKhachHang} mua SP ID ${maSanPham}...`);

  const client = await pool.connect();
  try {
    await client.query('BEGIN'); // Bắt đầu khóa giao dịch

    // Bước 1: Trừ tồn kho (Chỉ trừ nếu số lượng tồn >= số lượng mua)
    const sqlUpdateTonKho = `UPDATE SanPham SET SoLuongTon = SoLuongTon - $1 WHERE MaSanPham = $2 AND SoLuongTon >= $3`;
    const updateResult = await client.query(sqlUpdateTonKho, [soLuongMua, maSanPham, soLuongMua]);

    // Nếu không có dòng nào được cập nhật nghĩa là kho không đủ hàng
    if (updateResult.rowCount === 0) {
      console.log('⚠️ Thất bại: Kho không đủ hàng hoặc sản phẩm không tồn tại!');
      await client.query('ROLLBACK');
      return;
    }

    // Bước 2: Tạo Đơn hàng chính
    const tongTien = soLuongMua * donGia;
    const sqlInsertDonHang = `INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon) VALUES ($1, $2, 'Da dat hang') RETURNING MaDonHang`;
    const insertDon = await client.query(sqlInsertDonHang, [maKhachHang, tongTien]);
    const maDonHangMoi = insertDon.rows[0].madonhang; // Lấy ID của đơn hàng vừa tạo

    // Bước 3: Lưu Chi tiết đơn hàng
    const sqlInsertChiTiet = `INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia) VALUES ($1, $2, $3, $4)`;
    await client.query(sqlInsertChiTiet, [maDonHangMoi, maSanPham, soLuongMua, donGia]);

    // Bước 4: Chốt giao dịch thành công
    await client.query('COMMIT');
    console.log(`🎉 THÀNH CÔNG: Đã chốt đơn hàng #${maDonHangMoi}. Tồn kho đã được trừ an toàn!`);
  } catch (err) {
    console.log('❌ Lỗi hệ thống:', err.message);
    await client.query('ROLLBACK'); // Hủy bỏ toàn bộ thao tác
  } finally {
    client.release();
  }
}

// ==========================================
// KHU VỰC CHẠY THỬ NGHIỆM (TESTING)
// ==========================================

async function chayThuNghiem() {
  await initDatabase();

  // Thử thêm 1 sản phẩm vào kho (Giá 500k, Tồn kho 10 cái)
  await themSanPham('Bàn phím cơ Logitech', 500000, 10);

  // Khách hàng ID: 1, mua Sản phẩm ID: 1, số lượng: 2 cái, giá: 500000
  await datHang(1, 1, 2, 500000);

  // Cố tình cho một khách khác đặt lố 100 cái để xem hệ thống có chặn lại không nhé
  await datHang(2, 1, 100, 500000);

  await pool.end();
}

// Chỉ chạy thử nghiệm khi gọi trực tiếp `node src/index.js`
if (require.main === module) {
  chayThuNghiem().catch((err) => {
    console.error(err);
    process.exit(1);
  });
}
