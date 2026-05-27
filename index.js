const express = require('express');
const admin = require('firebase-admin');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;
const HOST = process.env.HOST || '0.0.0.0';
const serviceAccountPath = path.join(__dirname, 'serviceAccountKey.json');

app.use(express.json());

let firestore = null;

if (fs.existsSync(serviceAccountPath)) {
  const serviceAccount = require(serviceAccountPath);
  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
  });
  firestore = admin.firestore();
  console.log('Firebase da san sang.');
} else {
  console.warn('Chua co serviceAccountKey.json, server chay che do health-check/local.');
}

app.get('/', (req, res) => {
  res.json({
    message: 'SyncChain API is running',
    firebaseReady: Boolean(firestore),
  });
});

app.post('/api/orders', async (req, res) => {
  if (!firestore) {
    return res.status(503).json({
      error: 'Chua cau hinh Firebase. Dat serviceAccountKey.json vao thu muc goc project.',
    });
  }

  try {
    const { tenKhachHang, tenSanPham, soLuong } = req.body;
    const newOrderRef = firestore.collection('orders').doc();

    await newOrderRef.set({
      khachHang: tenKhachHang,
      sanPham: tenSanPham,
      soLuong,
      trangThai: 'Da dat hang',
      ngayDat: admin.firestore.FieldValue.serverTimestamp(),
    });

    res.status(200).json({
      message: 'Tao don hang thanh cong!',
      orderId: newOrderRef.id,
    });
  } catch (error) {
    res.status(500).json({ error: `Loi he thong: ${error.message}` });
  }
});

app.listen(PORT, HOST, () => {
  console.log(`SyncChain API dang chay tai http://${HOST}:${PORT}`);
});
