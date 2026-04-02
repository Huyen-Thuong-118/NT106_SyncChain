const fs = require('fs');
const sqlite3 = require('sqlite3').verbose();

const DB_FILE = './database/SyncChain.db';
const SQL_SCHEMA_FILE = './TaoBang.sql';

const db = new sqlite3.Database(DB_FILE, (err) => {
  if (err) {
    console.error('Lỗi kết nối SQLite:', err.message);
    process.exit(1);
  }
  console.log('✅ Đã kết nối SQLite:', DB_FILE);
  initDatabase();
});

function initDatabase() {
  const sql = fs.readFileSync(SQL_SCHEMA_FILE, 'utf8');
  db.exec(sql, (err) => {
    if (err) {
      console.error('Lỗi khởi tạo cấu trúc DB:', err.message);
      process.exit(1);
    }
    console.log('✅ Đã khởi tạo bảng (nếu chưa có).');
  });
}

/**
 * Hàm mẫu "Khách đặt hàng" (transaction bảo toàn).
 */
function datDonHang(maKhachHang, maSanPham, soLuong) {
  return new Promise((resolve, reject) => {
    db.serialize(() => {
      db.run('BEGIN TRANSACTION');

      db.get(
        'SELECT SoLuongTon, GiaBan FROM SanPham WHERE MaSanPham = ?',
        [maSanPham],
        (err, row) => {
          if (err) {
            db.run('ROLLBACK');
            return reject(err);
          }
          if (!row) {
            db.run('ROLLBACK');
            return reject(new Error('Sản phẩm không tồn tại.'));
          }
          if (row.SoLuongTon < soLuong) {
            db.run('ROLLBACK');
            return reject(new Error('Tồn kho không đủ.'));
          }

          const donGia = row.GiaBan;
          const tongTien = donGia * soLuong;

          // Giảm tồn kho
          db.run(
            'UPDATE SanPham SET SoLuongTon = SoLuongTon - ? WHERE MaSanPham = ?',
            [soLuong, maSanPham],
            function (err) {
              if (err) {
                db.run('ROLLBACK');
                return reject(err);
              }

              // Tạo đơn hàng
              db.run(
                'INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon) VALUES (?, ?, ?)',
                [maKhachHang, tongTien, 'DaDat'],
                function (err) {
                  if (err) {
                    db.run('ROLLBACK');
                    return reject(err);
                  }

                  const maDonHang = this.lastID;
                  db.run(
                    'INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia) VALUES (?, ?, ?, ?)',
                    [maDonHang, maSanPham, soLuong, donGia],
                    function (err) {
                      if (err) {
                        db.run('ROLLBACK');
                        return reject(err);
                      }

                      db.run('COMMIT', (err) => {
                        if (err) {
                          db.run('ROLLBACK');
                          return reject(err);
                        }
                        resolve({ maDonHang, tongTien });
                      });
                    }
                  );
                }
              );
            }
          );
        }
      );
    });
  });
}

// Example test (uncomment để chạy trực tiếp)
// datDonHang(1, 1, 2).then(console.log).catch(console.error);

module.exports = { db, datDonHang };

// ==========================================
// CÁC HÀM XỬ LÝ LOGIC CHO APP SYNCCHAIN
// ==========================================

// 1. Hàm Thêm Sản Phẩm (Mô phỏng Nhà phân phối nhập hàng mới lên app)
function themSanPham(ten, gia, soLuong) {
    const sql = `INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongTon) VALUES (?, ?, ?)`;
    
    // Dùng dấu ? để chống lỗi bảo mật SQL Injection
    db.run(sql, [ten, gia, soLuong], function(err) {
        if (err) {
            console.error("❌ Lỗi khi thêm sản phẩm:", err.message);
            return;
        }
        console.log(`✅ Đã thêm sản phẩm: [${ten}] - Mã SP: ${this.lastID} - Tồn kho: ${soLuong}`);
    });
}

// 2. Hàm Đặt Hàng An Toàn (Mô phỏng Khách hàng bấm mua)
// Hàm này dùng TRANSACTION để đảm bảo không bị lỗi "bán lố hàng" khi nhiều người mua cùng lúc
function datHang(maKhachHang, maSanPham, soLuongMua, donGia) {
    console.log(`\n⏳ Đang xử lý đơn hàng cho Khách ID ${maKhachHang} mua SP ID ${maSanPham}...`);
    
    db.serialize(() => {
        db.run("BEGIN TRANSACTION;"); // Bắt đầu khóa giao dịch

        // Bước 1: Trừ tồn kho (Chỉ trừ nếu số lượng tồn >= số lượng mua)
        const sqlUpdateTonKho = `UPDATE SanPham SET SoLuongTon = SoLuongTon - ? WHERE MaSanPham = ? AND SoLuongTon >= ?`;
        
        db.run(sqlUpdateTonKho, [soLuongMua, maSanPham, soLuongMua], function(err) {
            if (err) {
                console.log("❌ Lỗi hệ thống:", err.message);
                db.run("ROLLBACK;"); // Hủy bỏ toàn bộ thao tác
                return;
            }
            
            // Nếu không có dòng nào được cập nhật nghĩa là kho không đủ hàng
            if (this.changes === 0) {
                console.log("⚠️ Thất bại: Kho không đủ hàng hoặc sản phẩm không tồn tại!");
                db.run("ROLLBACK;"); 
                return;
            }

            // Bước 2: Tạo Đơn hàng chính
            let tongTien = soLuongMua * donGia;
            const sqlInsertDonHang = `INSERT INTO DonHang (MaKhachHang, TongTien, TrangThaiDon) VALUES (?, ?, 'Da dat hang')`;
            
            db.run(sqlInsertDonHang, [maKhachHang, tongTien], function(err) {
                if (err) {
                    console.log("❌ Lỗi tạo đơn hàng:", err.message);
                    db.run("ROLLBACK;");
                    return;
                }
                
                let maDonHangMoi = this.lastID; // Lấy ID của đơn hàng vừa tạo

                // Bước 3: Lưu Chi tiết đơn hàng
                const sqlInsertChiTiet = `INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia) VALUES (?, ?, ?, ?)`;
                
                db.run(sqlInsertChiTiet, [maDonHangMoi, maSanPham, soLuongMua, donGia], function(err) {
                    if (err) {
                        console.log("❌ Lỗi tạo chi tiết đơn:", err.message);
                        db.run("ROLLBACK;");
                        return;
                    }
                    
                    // Bước 4: Chốt giao dịch thành công
                    db.run("COMMIT;");
                    console.log(`🎉 THÀNH CÔNG: Đã chốt đơn hàng #${maDonHangMoi}. Tồn kho đã được trừ an toàn!`);
                });
            });
        });
    });
}

// ==========================================
// KHU VỰC CHẠY THỬ NGHIỆM (TESTING)
// ==========================================

// Đặt trong setTimeout để đảm bảo database kết nối xong mới chạy lệnh
setTimeout(() => {
    // Thử thêm 1 sản phẩm vào kho (Giá 500k, Tồn kho 10 cái)
    themSanPham("Bàn phím cơ Logitech", 500000, 10);

    // Chờ 1 giây sau đó cho khách hàng nhảy vào mua 2 cái
    setTimeout(() => {
        // Khách hàng ID: 1, mua Sản phẩm ID: 1, số lượng: 2 cái, giá: 500000
        datHang(1, 1, 2, 500000);
        
        // Cố tình cho một khách khác đặt lố 100 cái để xem hệ thống có chặn lại không nhé
        setTimeout(() => {
            datHang(2, 1, 100, 500000); 
        }, 500);

    }, 1000);
    
}, 1000);