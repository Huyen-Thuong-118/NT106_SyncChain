const express = require('express');
const bcrypt = require('bcrypt');
const db = require('./database');

const router = express.Router();

// ==========================================
// MIDDLEWARE
// ==========================================

// Middleware kiểm tra đăng nhập
function requireAuth(req, res, next) {
    if (req.session.user) {
        next();
    } else {
        res.status(401).json({ success: false, message: 'Vui lòng đăng nhập' });
    }
}

// Middleware kiểm tra admin
function requireAdmin(req, res, next) {
    if (req.session.user && req.session.user.role === 'admin') {
        next();
    } else {
        res.status(403).json({ success: false, message: 'Không có quyền truy cập' });
    }
}

// ==========================================
// API XÁC THỰC
// ==========================================

// 1. ĐĂNG KÝ
router.post('/register', async (req, res) => {
    const { username, email, password, confirmPassword } = req.body;
    
    if (!username || !email || !password || !confirmPassword) {
        return res.status(400).json({ success: false, message: 'Vui lòng điền đầy đủ thông tin' });
    }
    
    if (password !== confirmPassword) {
        return res.status(400).json({ success: false, message: 'Mật khẩu xác nhận không khớp' });
    }
    
    if (password.length < 6) {
        return res.status(400).json({ success: false, message: 'Mật khẩu phải có ít nhất 6 ký tự' });
    }
    
    try {
        // Kiểm tra username
        const existingUser = await new Promise((resolve, reject) => {
            db.get("SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = ?", [username], (err, row) => {
                if (err) reject(err);
                resolve(row);
            });
        });
        
        if (existingUser) {
            return res.status(400).json({ success: false, message: 'Tên đăng nhập đã tồn tại' });
        }
        
        // Kiểm tra email
        const existingEmail = await new Promise((resolve, reject) => {
            db.get("SELECT MaNguoiDung FROM NguoiDung WHERE Email = ?", [email], (err, row) => {
                if (err) reject(err);
                resolve(row);
            });
        });
        
        if (existingEmail) {
            return res.status(400).json({ success: false, message: 'Email đã được sử dụng' });
        }
        
        // Mã hóa mật khẩu
        const hashedPassword = await bcrypt.hash(password, 10);
        
        // Lưu user
        await new Promise((resolve, reject) => {
            db.run(
                "INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, Email) VALUES (?, ?, ?)",
                [username, hashedPassword, email],
                function(err) {
                    if (err) reject(err);
                    resolve(this.lastID);
                }
            );
        });
        
        res.json({ success: true, message: 'Đăng ký thành công! Vui lòng đăng nhập.' });
        
    } catch (error) {
        console.error('Register error:', error);
        res.status(500).json({ success: false, message: 'Lỗi server' });
    }
});

// 2. ĐĂNG NHẬP
router.post('/login', async (req, res) => {
    const { username, password, remember } = req.body;
    
    if (!username || !password) {
        return res.status(400).json({ success: false, message: 'Vui lòng nhập tên đăng nhập và mật khẩu' });
    }
    
    try {
        const user = await new Promise((resolve, reject) => {
            db.get(
                `SELECT u.*, p.TenVaiTro 
                 FROM NguoiDung u 
                 LEFT JOIN PhanQuyen p ON u.MaVaiTro = p.MaVaiTro 
                 WHERE u.TenDangNhap = ?`,
                [username],
                (err, row) => {
                    if (err) reject(err);
                    resolve(row);
                }
            );
        });
        
        if (!user) {
            return res.status(401).json({ success: false, message: 'Tài khoản không tồn tại' });
        }
        
        const isValidPassword = await bcrypt.compare(password, user.MatKhauHash);
        
        if (!isValidPassword) {
            return res.status(401).json({ success: false, message: 'Sai mật khẩu' });
        }
        
        // Lưu session
        req.session.user = {
            id: user.MaNguoiDung,
            username: user.TenDangNhap,
            email: user.Email,
            role: user.TenVaiTro || 'user'
        };
        
        if (remember) {
            req.session.cookie.maxAge = 30 * 24 * 60 * 60 * 1000;
        }
        
        res.json({ 
            success: true, 
            message: 'Đăng nhập thành công!',
            user: {
                username: user.TenDangNhap,
                email: user.Email,
                role: user.TenVaiTro
            }
        });
        
    } catch (error) {
        console.error('Login error:', error);
        res.status(500).json({ success: false, message: 'Lỗi server' });
    }
});

// 3. QUÊN MẬT KHẨU
router.post('/forgot-password', async (req, res) => {
    const { email } = req.body;
    
    if (!email) {
        return res.status(400).json({ success: false, message: 'Vui lòng nhập email' });
    }
    
    try {
        const user = await new Promise((resolve, reject) => {
            db.get("SELECT MaNguoiDung FROM NguoiDung WHERE Email = ?", [email], (err, row) => {
                if (err) reject(err);
                resolve(row);
            });
        });
        
        if (!user) {
            return res.status(404).json({ success: false, message: 'Email không tồn tại trong hệ thống' });
        }
        
        req.session.resetEmail = email;
        
        res.json({ success: true, message: 'Email hợp lệ! Vui lòng nhập mật khẩu mới.' });
        
    } catch (error) {
        console.error('Forgot password error:', error);
        res.status(500).json({ success: false, message: 'Lỗi server' });
    }
});

// 4. ĐẶT LẠI MẬT KHẨU
router.post('/reset-password', async (req, res) => {
    const { newPassword, confirmPassword } = req.body;
    const email = req.session.resetEmail;
    
    if (!email) {
        return res.status(400).json({ success: false, message: 'Phiên đặt lại mật khẩu đã hết hạn' });
    }
    
    if (!newPassword || !confirmPassword) {
        return res.status(400).json({ success: false, message: 'Vui lòng nhập mật khẩu mới' });
    }
    
    if (newPassword !== confirmPassword) {
        return res.status(400).json({ success: false, message: 'Mật khẩu xác nhận không khớp' });
    }
    
    if (newPassword.length < 6) {
        return res.status(400).json({ success: false, message: 'Mật khẩu phải có ít nhất 6 ký tự' });
    }
    
    try {
        const hashedPassword = await bcrypt.hash(newPassword, 10);
        
        await new Promise((resolve, reject) => {
            db.run(
                "UPDATE NguoiDung SET MatKhauHash = ? WHERE Email = ?",
                [hashedPassword, email],
                function(err) {
                    if (err) reject(err);
                    resolve(this.changes);
                }
            );
        });
        
        delete req.session.resetEmail;
        
        res.json({ success: true, message: 'Đặt lại mật khẩu thành công!' });
        
    } catch (error) {
        console.error('Reset password error:', error);
        res.status(500).json({ success: false, message: 'Lỗi server' });
    }
});

// 5. ĐĂNG XUẤT
router.post('/logout', (req, res) => {
    req.session.destroy((err) => {
        if (err) {
            return res.status(500).json({ success: false, message: 'Lỗi khi đăng xuất' });
        }
        res.json({ success: true, message: 'Đăng xuất thành công!' });
    });
});

// 6. LẤY THÔNG TIN USER
router.get('/me', requireAuth, (req, res) => {
    res.json({ success: true, user: req.session.user });
});

// 7. KIỂM TRA ĐĂNG NHẬP
router.get('/check', (req, res) => {
    if (req.session.user) {
        res.json({ success: true, isAuthenticated: true, user: req.session.user });
    } else {
        res.json({ success: true, isAuthenticated: false });
    }
});

// Export middleware để dùng ở file khác
module.exports = { router, requireAuth, requireAdmin };