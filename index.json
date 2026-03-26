const express = require('express');
const admin = require('firebase-admin');

// 1. Kết nối Firebase bằng file Key
const serviceAccount = require('./serviceAccountKey.json');
admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

const db = admin.firestore();
const app = express();
app.use(express.json()); // Cấu hình để Web Service đọc được dữ liệu JSON

// 2. Viết API Tạo Đơn Hàng (Bám sát kế hoạch của bạn)
app.post('/api/orders', async (req, res) => {
    try {
        // Lấy dữ liệu do khách hàng/đại lý gửi lên
        const { tenKhachHang, tenSanPham, soLuong } = req.body;

        // Bắt đầu luồng: Tạo đơn hàng -> Lưu vào Firebase
        const newOrderRef = db.collection('orders').doc(); // Tự động tạo ID đơn hàng
        
        await newOrderRef.set({
            khachHang: tenKhachHang,
            sanPham: tenSanPham,
            soLuong: soLuong,
            trangThai: "Đã đặt hàng", // Cập nhật trạng thái chuẩn như kế hoạch
            ngayDat: admin.firestore.FieldValue.serverTimestamp()
        });

        // Phản hồi về cho Client
        res.status(200).json({
            message: "Tạo đơn hàng thành công!",
            orderId: newOrderRef.id
        });
        
    } catch (error) {
        res.status(500).json({ error: "Lỗi hệ thống: " + error.message });
    }
});

// 3. Chạy Web Service ở cổng 3000
const PORT = 3000;
app.listen(PORT, () => {
    console.log(`🚀 Web Service đang chạy tại http://localhost:${PORT}`);
});