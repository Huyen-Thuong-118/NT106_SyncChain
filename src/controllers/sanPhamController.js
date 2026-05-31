// src/controllers/sanPhamController.js
// Xử lý request/response cho sản phẩm; logic DB nằm ở sanPhamModel.
const sanPhamModel = require('../models/sanPhamModel');

// GET /api/sanpham
async function getAll(req, res) {
  try {
    const data = await sanPhamModel.layTatCa();
    res.json({ success: true, data });
  } catch (err) {
    console.error('Lỗi truy vấn SanPham:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn dữ liệu sản phẩm' });
  }
}

// POST /api/sanpham
async function create(req, res) {
  const { TenSanPham, GiaBan, SoLuongTon, MucTonThap } = req.body;

  if (!TenSanPham || GiaBan == null || SoLuongTon == null) {
    return res.status(400).json({
      success: false,
      message: 'Thiếu thông tin: cần TenSanPham, GiaBan, SoLuongTon',
    });
  }
  if (GiaBan < 0 || SoLuongTon < 0) {
    return res.status(400).json({ success: false, message: 'GiaBan và SoLuongTon không được âm' });
  }

  try {
    const data = await sanPhamModel.them({ TenSanPham, GiaBan, SoLuongTon, MucTonThap });
    res.status(201).json({ success: true, data });
  } catch (err) {
    console.error('Lỗi thêm SanPham:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi thêm sản phẩm' });
  }
}

module.exports = { getAll, create };
