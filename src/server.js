const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { pool } = require('./db');
const { ORDER_STATUS } = require('./constants/statusEnum');

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

// ==========================================
// SẢN PHẨM
// ==========================================

// GET /api/sanpham: danh sách tất cả sản phẩm.
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
    ORDER BY MaSanPham
  `;
  try {
    const result = await pool.query(sql);
    res.json({ success: true, data: result.rows });
  } catch (err) {
    console.error('Lỗi truy vấn SanPham:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn dữ liệu sản phẩm' });
  }
});

// POST /api/sanpham: thêm sản phẩm mới (Nhà phân phối nhập hàng).
// Body: { TenSanPham, GiaBan, SoLuongTon, MucTonThap? }
app.post('/api/sanpham', async (req, res) => {
  const { TenSanPham, GiaBan, SoLuongTon, MucTonThap } = req.body;

  // Kiểm tra dữ liệu đầu vào
  if (!TenSanPham || GiaBan == null || SoLuongTon == null) {
    return res.status(400).json({
      success: false,
      message: 'Thiếu thông tin: cần TenSanPham, GiaBan, SoLuongTon',
    });
  }
  if (GiaBan < 0 || SoLuongTon < 0) {
    return res.status(400).json({ success: false, message: 'GiaBan và SoLuongTon không được âm' });
  }

  const sql = `
    INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon, MucTonThap)
    VALUES ($1, $2, $3, COALESCE($4, 10))
    RETURNING MaSanPham  AS "MaSanPham",
              TenSanPham AS "TenSanPham",
              GiaBan     AS "GiaBan",
              SoLuongTon AS "SoLuongTon",
              MucTonThap AS "MucTonThap",
              TrangThai  AS "TrangThai"
  `;
  try {
    const result = await pool.query(sql, [TenSanPham, GiaBan, SoLuongTon, MucTonThap]);
    res.status(201).json({ success: true, data: result.rows[0] });
  } catch (err) {
    console.error('Lỗi thêm SanPham:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi thêm sản phẩm' });
  }
});

// ==========================================
// ĐƠN HÀNG
// ==========================================

// POST /api/donhang: tạo đơn hàng (Khách hàng đặt mua).
// Body: { MaKhachHang, items: [ { MaSanPham, SoLuong }, ... ] }
// Dùng TRANSACTION: trừ tồn kho an toàn khi nhiều client mua cùng lúc,
// và lấy GiaBan từ DB (không tin giá do client gửi lên).
app.post('/api/donhang', async (req, res) => {
  const { MaKhachHang, items } = req.body;

  if (!MaKhachHang || !Array.isArray(items) || items.length === 0) {
    return res.status(400).json({
      success: false,
      message: 'Thiếu thông tin: cần MaKhachHang và mảng items không rỗng',
    });
  }
  for (const item of items) {
    if (!item || item.MaSanPham == null || !(item.SoLuong > 0)) {
      return res.status(400).json({
        success: false,
        message: 'Mỗi item cần MaSanPham và SoLuong > 0',
      });
    }
  }

  const client = await pool.connect();
  try {
    await client.query('BEGIN');

    let tongTien = 0;
    const chiTiet = [];

    // Trừ tồn kho từng mặt hàng; chỉ trừ khi đủ tồn (SoLuongTon >= SoLuong).
    for (const { MaSanPham, SoLuong } of items) {
      const upd = await client.query(
        `UPDATE SanPham
           SET SoLuongTon = SoLuongTon - $1
         WHERE MaSanPham = $2 AND SoLuongTon >= $1
         RETURNING GiaBan`,
        [SoLuong, MaSanPham]
      );
      if (upd.rowCount === 0) {
        // Không đủ tồn hoặc sản phẩm không tồn tại -> hủy toàn bộ giao dịch.
        await client.query('ROLLBACK');
        return res.status(409).json({
          success: false,
          message: `Không đủ tồn kho hoặc sản phẩm không tồn tại (MaSanPham=${MaSanPham})`,
        });
      }
      const donGia = Number(upd.rows[0].giaban);
      tongTien += donGia * SoLuong;
      chiTiet.push({ MaSanPham, SoLuong, DonGia: donGia });
    }

    // Tạo đơn hàng chính - trạng thái khởi tạo lấy từ statusEnum.js (Draft).
    const insDon = await client.query(
      `INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon)
       VALUES ($1, $2, $3)
       RETURNING MaDonHang`,
      [MaKhachHang, tongTien, ORDER_STATUS.DRAFT]
    );
    const maDonHang = insDon.rows[0].madonhang;

    // Lưu chi tiết đơn hàng
    for (const { MaSanPham, SoLuong, DonGia } of chiTiet) {
      await client.query(
        `INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia)
         VALUES ($1, $2, $3, $4)`,
        [maDonHang, MaSanPham, SoLuong, DonGia]
      );
    }

    await client.query('COMMIT');
    res.status(201).json({
      success: true,
      data: { MaDonHang: maDonHang, TongTien: tongTien, items: chiTiet },
    });
  } catch (err) {
    await client.query('ROLLBACK');
    console.error('Lỗi tạo đơn hàng:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi tạo đơn hàng' });
  } finally {
    client.release();
  }
});

// GET /api/donhang: danh sách đơn hàng (mới nhất trước).
app.get('/api/donhang', async (req, res) => {
  const sql = `
    SELECT MaDonHang    AS "MaDonHang",
           MaKhachHang  AS "MaKhachHang",
           TongTien     AS "TongTien",
           TrangThaiDon AS "TrangThaiDon",
           NgayTao      AS "NgayTao"
    FROM DonHang
    ORDER BY MaDonHang DESC
  `;
  try {
    const result = await pool.query(sql);
    res.json({ success: true, data: result.rows });
  } catch (err) {
    console.error('Lỗi truy vấn DonHang:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn đơn hàng' });
  }
});

// GET /api/donhang/:id: 1 đơn hàng kèm chi tiết các mặt hàng.
app.get('/api/donhang/:id', async (req, res) => {
  const maDonHang = Number(req.params.id);
  if (!Number.isInteger(maDonHang)) {
    return res.status(400).json({ success: false, message: 'Mã đơn hàng không hợp lệ' });
  }
  try {
    const donResult = await pool.query(
      `SELECT MaDonHang    AS "MaDonHang",
              MaKhachHang  AS "MaKhachHang",
              TongTien     AS "TongTien",
              TrangThaiDon AS "TrangThaiDon",
              NgayTao      AS "NgayTao"
       FROM DonHang WHERE MaDonHang = $1`,
      [maDonHang]
    );
    if (donResult.rowCount === 0) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy đơn hàng' });
    }

    const ctResult = await pool.query(
      `SELECT ct.MaSanPham  AS "MaSanPham",
              sp.TenSanPham AS "TenSanPham",
              ct.SoLuong    AS "SoLuong",
              ct.DonGia     AS "DonGia"
       FROM ChiTietDonHang ct
       JOIN SanPham sp ON sp.MaSanPham = ct.MaSanPham
       WHERE ct.MaDonHang = $1`,
      [maDonHang]
    );

    res.json({ success: true, data: { ...donResult.rows[0], items: ctResult.rows } });
  } catch (err) {
    console.error('Lỗi truy vấn chi tiết đơn hàng:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn chi tiết đơn hàng' });
  }
});

// ==========================================
// KHỞI ĐỘNG SERVER
// ==========================================
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
