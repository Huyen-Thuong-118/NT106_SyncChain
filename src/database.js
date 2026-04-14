const sqlite3 = require('sqlite3').verbose();
const fs = require('fs');
const path = require('path');

const DB_FILE = path.join(__dirname, '../database/SyncChain.db');
const SQL_SCHEMA_FILE = path.join(__dirname, '../database/TaoBang.sql');

const db = new sqlite3.Database(DB_FILE, (err) => {
    if (err) {
        console.error('❌ Lỗi kết nối SQLite:', err.message);
        process.exit(1);
    }
    console.log('✅ Đã kết nối SQLite:', DB_FILE);
});

// Hàm khởi tạo database
function initDatabase() {
    // Đọc schema gốc
    if (fs.existsSync(SQL_SCHEMA_FILE)) {
        const sql = fs.readFileSync(SQL_SCHEMA_FILE, 'utf8');
        db.exec(sql, (err) => {
            if (err) {
                console.error('Lỗi khởi tạo schema:', err.message);
            } else {
                console.log('✅ Schema chính đã khởi tạo');
            }
        });
    }
    
    // Tạo bảng xác thực
    const authTables = `
        CREATE TABLE IF NOT EXISTS PhanQuyen (
            MaVaiTro INTEGER PRIMARY KEY AUTOINCREMENT,
            TenVaiTro TEXT NOT NULL UNIQUE
        );
        
        CREATE TABLE IF NOT EXISTS NguoiDung (
            MaNguoiDung INTEGER PRIMARY KEY AUTOINCREMENT,
            TenDangNhap TEXT UNIQUE NOT NULL,
            MatKhauHash TEXT NOT NULL,
            Email TEXT UNIQUE NOT NULL,
            MaVaiTro INTEGER DEFAULT 1,
            NgayTao DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (MaVaiTro) REFERENCES PhanQuyen(MaVaiTro)
        );
        
        INSERT OR IGNORE INTO PhanQuyen (MaVaiTro, TenVaiTro) VALUES (1, 'user');
        INSERT OR IGNORE INTO PhanQuyen (MaVaiTro, TenVaiTro) VALUES (2, 'admin');
    `;
    
    db.exec(authTables, (err) => {
        if (err) {
            console.error('Lỗi tạo bảng xác thực:', err.message);
        } else {
            console.log('✅ Bảng xác thực đã sẵn sàng');
        }
    });
}

// Gọi khởi tạo
initDatabase();

module.exports = db;