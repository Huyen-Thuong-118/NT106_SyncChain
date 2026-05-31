// src/models/donHangModel.js
// Tầng truy vấn dữ liệu cho bảng DonHang (gồm transaction đặt hàng).
const { pool } = require('../db');
const { ORDER_STATUS } = require('../constants/statusEnum');

const COLS = `
  MaDonHang    AS "MaDonHang",
  MaKhachHang  AS "MaKhachHang",
  TongTien     AS "TongTien",
  TrangThaiDon AS "TrangThaiDon",
  MaVanDon     AS "MaVanDon",
  DonViVanChuyen AS "DonViVanChuyen",
  NgayTao      AS "NgayTao"
`;

// Tạo đơn hàng trong 1 TRANSACTION: trừ tồn kho an toàn khi nhiều client
// mua cùng lúc, lấy GiaBan từ DB (không tin giá client gửi lên).
// Ném lỗi có .status = 409 nếu không đủ tồn / sản phẩm không tồn tại.
async function taoDonHang({ MaKhachHang, items, NguoiNhan, SoDienThoaiNhan, DiaChiGiao }) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');

    let tongTien = 0;
    const chiTiet = [];

    for (const { MaSanPham, SoLuong } of items) {
      const upd = await client.query(
        `UPDATE SanPham
           SET SoLuongTon = SoLuongTon - $1
         WHERE MaSanPham = $2 AND SoLuongTon >= $1
         RETURNING GiaBan`,
        [SoLuong, MaSanPham]
      );
      if (upd.rowCount === 0) {
        const err = new Error(`Không đủ tồn kho hoặc sản phẩm không tồn tại (MaSanPham=${MaSanPham})`);
        err.status = 409;
        throw err;
      }
      const donGia = Number(upd.rows[0].giaban);
      tongTien += donGia * SoLuong;
      chiTiet.push({ MaSanPham, SoLuong, DonGia: donGia });
    }

    const insDon = await client.query(
      `INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon, NguoiNhan, SoDienThoaiNhan, DiaChiGiao)
       VALUES ($1, $2, $3, $4, $5, $6)
       RETURNING MaDonHang`,
      [MaKhachHang, tongTien, ORDER_STATUS.DA_DAT_HANG, NguoiNhan || null, SoDienThoaiNhan || null, DiaChiGiao || null]
    );
    const maDonHang = insDon.rows[0].madonhang;

    for (const { MaSanPham, SoLuong, DonGia } of chiTiet) {
      await client.query(
        `INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia)
         VALUES ($1, $2, $3, $4)`,
        [maDonHang, MaSanPham, SoLuong, DonGia]
      );
    }

    await client.query('COMMIT');
    return { MaDonHang: maDonHang, TongTien: tongTien, items: chiTiet };
  } catch (err) {
    await client.query('ROLLBACK');
    throw err;
  } finally {
    client.release();
  }
}

// Lấy danh sách đơn. Nếu truyền maKhachHang -> chỉ đơn của khách đó (khách chỉ xem đơn mình).
async function layTatCa(maKhachHang = null) {
  if (maKhachHang) {
    const { rows } = await pool.query(
      `SELECT ${COLS} FROM DonHang WHERE MaKhachHang = $1 ORDER BY MaDonHang DESC`,
      [maKhachHang]
    );
    return rows;
  }
  const { rows } = await pool.query(`SELECT ${COLS} FROM DonHang ORDER BY MaDonHang DESC`);
  return rows;
}

// Trả về đơn hàng kèm chi tiết các mặt hàng, hoặc null nếu không tìm thấy.
async function layTheoId(maDonHang) {
  const don = await pool.query(`SELECT ${COLS} FROM DonHang WHERE MaDonHang = $1`, [maDonHang]);
  if (don.rowCount === 0) return null;

  const ct = await pool.query(
    `SELECT ct.MaSanPham  AS "MaSanPham",
            sp.TenSanPham AS "TenSanPham",
            ct.SoLuong    AS "SoLuong",
            ct.DonGia     AS "DonGia"
     FROM ChiTietDonHang ct
     JOIN SanPham sp ON sp.MaSanPham = ct.MaSanPham
     WHERE ct.MaDonHang = $1`,
    [maDonHang]
  );
  return { ...don.rows[0], items: ct.rows };
}

// Cập nhật trạng thái đơn (kèm thông tin vận đơn khi chuyển sang vận chuyển).
async function capNhatTrangThai(maDonHang, trangThaiMoi, { MaVanDon = null, DonViVanChuyen = null } = {}) {
  const { rows } = await pool.query(
    `UPDATE DonHang
       SET TrangThaiDon   = $1,
           MaVanDon       = COALESCE($2, MaVanDon),
           DonViVanChuyen = COALESCE($3, DonViVanChuyen),
           NgayCapNhat    = CURRENT_TIMESTAMP
     WHERE MaDonHang = $4
     RETURNING ${COLS}`,
    [trangThaiMoi, MaVanDon, DonViVanChuyen, maDonHang]
  );
  return rows[0];
}

module.exports = { taoDonHang, layTatCa, layTheoId, capNhatTrangThai };
