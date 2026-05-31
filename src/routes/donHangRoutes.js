// src/routes/donHangRoutes.js
const express = require('express');
const router = express.Router();
const ctrl = require('../controllers/donHangController');
const { xacThuc, phanQuyen } = require('../middleware/auth');

// Mọi thao tác đơn hàng đều yêu cầu đăng nhập.
router.post('/', xacThuc, ctrl.create);     // đặt hàng (khách)
router.get('/', xacThuc, ctrl.getAll);      // khách xem đơn mình; nhân viên xem tất cả
router.get('/:id', xacThuc, ctrl.getById);  // chi tiết 1 đơn

// Đổi trạng thái đơn: chỉ nhân viên kho / nhà phân phối / admin.
router.patch(
  '/:id/trangthai',
  xacThuc,
  phanQuyen('NhanVienKho', 'NhaPhanPhoi', 'Admin'),
  ctrl.capNhatTrangThai
);

module.exports = router;
