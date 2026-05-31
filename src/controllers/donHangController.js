// src/controllers/donHangController.js
// Xử lý request/response cho đơn hàng; logic DB nằm ở donHangModel.
const donHangModel = require('../models/donHangModel');

// POST /api/donhang
async function create(req, res) {
  const { MaKhachHang, items } = req.body;

  if (!MaKhachHang || !Array.isArray(items) || items.length === 0) {
    return res.status(400).json({
      success: false,
      message: 'Thiếu thông tin: cần MaKhachHang và mảng items không rỗng',
    });
  }
  for (const item of items) {
    if (!item || item.MaSanPham == null || !(item.SoLuong > 0)) {
      return res.status(400).json({ success: false, message: 'Mỗi item cần MaSanPham và SoLuong > 0' });
    }
  }

  try {
    const data = await donHangModel.taoDonHang({ MaKhachHang, items });
    res.status(201).json({ success: true, data });
  } catch (err) {
    // Lỗi nghiệp vụ (vd hết hàng -> 409) được model gắn err.status.
    if (err.status) {
      return res.status(err.status).json({ success: false, message: err.message });
    }
    console.error('Lỗi tạo đơn hàng:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi tạo đơn hàng' });
  }
}

// GET /api/donhang
async function getAll(req, res) {
  try {
    const data = await donHangModel.layTatCa();
    res.json({ success: true, data });
  } catch (err) {
    console.error('Lỗi truy vấn DonHang:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn đơn hàng' });
  }
}

// GET /api/donhang/:id
async function getById(req, res) {
  const maDonHang = Number(req.params.id);
  if (!Number.isInteger(maDonHang)) {
    return res.status(400).json({ success: false, message: 'Mã đơn hàng không hợp lệ' });
  }
  try {
    const data = await donHangModel.layTheoId(maDonHang);
    if (!data) return res.status(404).json({ success: false, message: 'Không tìm thấy đơn hàng' });
    res.json({ success: true, data });
  } catch (err) {
    console.error('Lỗi truy vấn chi tiết đơn hàng:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn chi tiết đơn hàng' });
  }
}

module.exports = { create, getAll, getById };
