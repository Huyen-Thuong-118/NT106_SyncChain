// src/controllers/authController.js
// Đăng ký / đăng nhập. Mật khẩu băm bằng bcrypt, phiên đăng nhập bằng JWT.
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const nguoiDungModel = require('../models/nguoiDungModel');
const lichSuModel = require('../models/lichSuModel');
const { JWT_SECRET, JWT_EXPIRES } = require('../config');

function taoToken(nd) {
  return jwt.sign(
    { MaNguoiDung: nd.MaNguoiDung, TenDangNhap: nd.TenDangNhap, VaiTro: nd.VaiTro },
    JWT_SECRET,
    { expiresIn: JWT_EXPIRES }
  );
}

// POST /api/auth/register — khách hàng tự đăng ký (mặc định vai trò KhachHang).
async function register(req, res) {
  const { TenDangNhap, MatKhau, Email } = req.body;
  if (!TenDangNhap || !MatKhau || !Email) {
    return res.status(400).json({ success: false, message: 'Cần TenDangNhap, MatKhau, Email' });
  }
  if (MatKhau.length < 6) {
    return res.status(400).json({ success: false, message: 'Mật khẩu tối thiểu 6 ký tự' });
  }
  try {
    const hash = await bcrypt.hash(MatKhau, 10);
    const nd = await nguoiDungModel.taoNguoiDung({
      TenDangNhap, MatKhauHash: hash, Email, tenVaiTro: 'KhachHang',
    });
    res.status(201).json({ success: true, data: nd });
  } catch (err) {
    if (err.code === '23505') { // unique_violation
      return res.status(409).json({ success: false, message: 'Tên đăng nhập hoặc email đã tồn tại' });
    }
    console.error('Lỗi đăng ký:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi đăng ký' });
  }
}

// POST /api/auth/login — trả về JWT nếu đúng tài khoản.
async function login(req, res) {
  const { TenDangNhap, MatKhau } = req.body;
  if (!TenDangNhap || !MatKhau) {
    return res.status(400).json({ success: false, message: 'Cần TenDangNhap và MatKhau' });
  }
  try {
    const nd = await nguoiDungModel.timTheoTenDangNhap(TenDangNhap);
    // So sánh hằng thời gian; không tiết lộ sai tên hay sai mật khẩu.
    if (!nd || !(await bcrypt.compare(MatKhau, nd.MatKhauHash))) {
      return res.status(401).json({ success: false, message: 'Sai tên đăng nhập hoặc mật khẩu' });
    }
    await nguoiDungModel.capNhatLanDangNhap(nd.MaNguoiDung);
    await lichSuModel.ghi(nd.MaNguoiDung, 'DANG_NHAP', 'Đăng nhập thành công', req.ip);
    const token = taoToken(nd);
    res.json({
      success: true,
      data: {
        token,
        nguoiDung: { MaNguoiDung: nd.MaNguoiDung, TenDangNhap: nd.TenDangNhap, VaiTro: nd.VaiTro },
      },
    });
  } catch (err) {
    console.error('Lỗi đăng nhập:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi đăng nhập' });
  }
}

module.exports = { register, login };
