// src/routes/sanPhamRoutes.js
const express = require('express');
const router = express.Router();
const ctrl = require('../controllers/sanPhamController');

router.get('/', ctrl.getAll);   // GET  /api/sanpham
router.post('/', ctrl.create);  // POST /api/sanpham

module.exports = router;
