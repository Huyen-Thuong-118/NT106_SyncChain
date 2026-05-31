// src/routes/donHangRoutes.js
const express = require('express');
const router = express.Router();
const ctrl = require('../controllers/donHangController');

router.post('/', ctrl.create);     // POST /api/donhang
router.get('/', ctrl.getAll);      // GET  /api/donhang
router.get('/:id', ctrl.getById);  // GET  /api/donhang/:id

module.exports = router;
