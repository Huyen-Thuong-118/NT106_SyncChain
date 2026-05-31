// src/routes/authRoutes.js
const express = require('express');
const router = express.Router();
const ctrl = require('../controllers/authController');

router.post('/register', ctrl.register);  // POST /api/auth/register
router.post('/login', ctrl.login);        // POST /api/auth/login

module.exports = router;
