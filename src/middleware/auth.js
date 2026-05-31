// src/middleware/auth.js
// Middleware xác thực JWT và phân quyền theo vai trò.
const jwt = require('jsonwebtoken');
const { JWT_SECRET } = require('../config');

// Yêu cầu request có token hợp lệ ở header: Authorization: Bearer <token>
function xacThuc(req, res, next) {
  const header = req.headers.authorization || '';
  const token = header.startsWith('Bearer ') ? header.slice(7) : null;
  if (!token) {
    return res.status(401).json({ success: false, message: 'Thiếu token đăng nhập' });
  }
  try {
    // Gắn thông tin người dùng giải mã được vào req để controller dùng tiếp.
    req.nguoiDung = jwt.verify(token, JWT_SECRET);
    next();
  } catch (err) {
    return res.status(401).json({ success: false, message: 'Token không hợp lệ hoặc đã hết hạn' });
  }
}

// Chỉ cho phép các vai trò chỉ định. Dùng SAU xacThuc.
// Ví dụ: phanQuyen('NhaPhanPhoi', 'Admin')
function phanQuyen(...vaiTroChoPhep) {
  return (req, res, next) => {
    if (!req.nguoiDung || !vaiTroChoPhep.includes(req.nguoiDung.VaiTro)) {
      return res.status(403).json({ success: false, message: 'Bạn không có quyền thực hiện thao tác này' });
    }
    next();
  };
}

module.exports = { xacThuc, phanQuyen };
