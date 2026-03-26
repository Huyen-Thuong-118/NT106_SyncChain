const express = require('express');
const admin = require('firebase-admin');

// 1. Kết nối Firebase
const serviceAccount = require('./serviceAccountKey.json');
admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

const db = admin.firestore();
const app = express();
app.use(express.json());

// 2. API Xử lý đặt hàng cốt lõi (Kiểm tra -> Khóa -> Tạo đơn)
app.post('/api/orders', async (req, res) => {
    const { tenKhachHang, productId, soLuong } = req.body;
    
    const productRef = db.collection('products').doc(productId);
    const newOrderRef = db.collection('orders').doc(); 

    try {
        await db.runTransaction(async (t) => {
            const doc = await t.get(productRef);
            if (!doc.exists) {
                throw new Error("Sản phẩm này không tồn tại trong hệ thống!");
            }

            // SỬA Ở ĐÂY: Trỏ chính xác vào chữ 'TonKho' (T viết hoa) trên Firebase
            const tonKhoHienTai = doc.data().TonKho;

            // Thêm lớp khiên bảo vệ: Chặn đứng nếu Firebase bị lỗi NaN hoặc trống dữ liệu
            if (tonKhoHienTai === undefined || isNaN(tonKhoHienTai)) {
                throw new Error("Dữ liệu tồn kho trên Database đang bị lỗi (NaN/Undefined)!");
            }

            if (tonKhoHienTai < soLuong) {
                throw new Error(`Cảnh báo: Tồn kho không đủ! Chỉ còn ${tonKhoHienTai} sản phẩm.`);
            }

            // SỬA Ở ĐÂY: Ra lệnh trừ đúng cái trường 'TonKho' viết hoa
            t.update(productRef, { TonKho: tonKhoHienTai - soLuong });

            t.set(newOrderRef, {
                khachHang: tenKhachHang,
                productId: productId,
                soLuong: soLuong,
                trangThai: "Đã đặt hàng",
                ngayDat: admin.firestore.FieldValue.serverTimestamp()
            });
        });

        res.status(200).json({
            status: "success",
            message: "Đặt hàng thành công! Đã đồng bộ trừ tồn kho an toàn.",
            orderId: newOrderRef.id
        });
        
    } catch (error) {
        res.status(400).json({ 
            status: "error",
            message: error.message 
        });
    }
});

// 3. Khởi động Server
const PORT = 3000;
app.listen(PORT, () => {
    console.log(`🚀 Web Service đang chạy tại http://localhost:${PORT}`);
});