// src/routes/sanPhamRoutes.js
const express = require('express');
const router = express.Router();
const ctrl = require('../controllers/sanPhamController');
const { xacThuc, phanQuyen } = require('../middleware/auth');

router.get('/', ctrl.getAll); // GET /api/sanpham — công khai, ai cũng xem được catalog

// Thêm sản phẩm: chỉ nhà phân phối / admin.
router.post('/', xacThuc, phanQuyen('NhaPhanPhoi', 'Admin'), ctrl.create);

module.exports = router;
