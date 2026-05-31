// src/models/donHangModel.js
// Tầng truy vấn dữ liệu cho bảng DonHang (gồm transaction đặt hàng).
const { pool } = require('../db');
const { ORDER_STATUS } = require('../constants/statusEnum');

const COLS = `
  MaDonHang    AS "MaDonHang",
  MaKhachHang  AS "MaKhachHang",
  TongTien     AS "TongTien",
  TrangThaiDon AS "TrangThaiDon",
  NgayTao      AS "NgayTao"
`;

// Tạo đơn hàng trong 1 TRANSACTION: trừ tồn kho an toàn khi nhiều client
// mua cùng lúc, lấy GiaBan từ DB (không tin giá client gửi lên).
// Ném lỗi có .status = 409 nếu không đủ tồn / sản phẩm không tồn tại.
async function taoDonHang({ MaKhachHang, items }) {
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
      `INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon)
       VALUES ($1, $2, $3)
       RETURNING MaDonHang`,
      [MaKhachHang, tongTien, ORDER_STATUS.DA_DAT_HANG]
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

async function layTatCa() {
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

module.exports = { taoDonHang, layTatCa, layTheoId };
