// src/seedDatabase.js
const admin = require('firebase-admin');
const serviceAccount = require('../serviceAccountKey.json');

admin.initializeApp({ credential: admin.credential.cert(serviceAccount) });
const db = admin.firestore();

async function seed() {
  console.log('Bắt đầu seed dữ liệu...');

  // --- Users ---
  const users = {
    'user-admin-001': { uid: 'user-admin-001', email: 'admin@syncchain.vn', displayName: 'Admin Hệ thống', role: 'admin', isActive: true, createdAt: admin.firestore.FieldValue.serverTimestamp() },
    'user-staff-001': { uid: 'user-staff-001', email: 'staff@syncchain.vn', displayName: 'Nhân viên Kho', role: 'warehouse_staff', isActive: true, createdAt: admin.firestore.FieldValue.serverTimestamp() },
    'user-sup-001':   { uid: 'user-sup-001',   email: 'supplier@abc.com',  displayName: 'NCC Công ty ABC',  role: 'supplier', isActive: true, createdAt: admin.firestore.FieldValue.serverTimestamp() },
    'user-cus-001':   { uid: 'user-cus-001',   email: 'customer@gmail.com', displayName: 'Khách Nguyễn Văn A', role: 'customer', isActive: true, createdAt: admin.firestore.FieldValue.serverTimestamp() },
  };
  for (const [id, data] of Object.entries(users)) {
    await db.collection('users').doc(id).set(data);
  }

  // --- Categories & Brands ---
  await db.collection('categories').doc('cat-001').set({ name: 'Điện thoại', description: 'Thiết bị di động', createdAt: admin.firestore.FieldValue.serverTimestamp() });
  await db.collection('brands').doc('brand-001').set({ name: 'Samsung', categoryId: 'cat-001', createdAt: admin.firestore.FieldValue.serverTimestamp() });

  // --- Suppliers ---
  await db.collection('suppliers').doc('sup-001').set({ name: 'Công ty ABC Electronics', contactEmail: 'supplier@abc.com', phone: '0901234567', address: 'Q1, TP.HCM', isActive: true });

  // --- Warehouses ---
  await db.collection('warehouses').doc('wh-001').set({ name: 'Kho chính HCM', address: 'Q7, TP.HCM', bins: ['A1', 'A2', 'B1', 'B2'] });

  // --- SKUs ---
  await db.collection('skus').doc('sku-001').set({
    code: 'SKU-001', name: 'Samsung Galaxy S24', categoryId: 'cat-001', brandId: 'brand-001',
    supplierId: 'sup-001', unit: 'chiếc', price: 20000000, stockQty: 100,
    lowStockThreshold: 10, warehouseId: 'wh-001', binLocation: 'A1', isActive: true,
    createdAt: admin.firestore.FieldValue.serverTimestamp()
  });

  // --- Purchase Orders (PO) — đủ các trạng thái ---
  const poStatuses = ['Draft', 'Approved', 'Processing', 'Done', 'Cancelled'];
  for (let i = 0; i < poStatuses.length; i++) {
    await db.collection('purchaseOrders').doc(`po-00${i+1}`).set({
      poCode: `PO-2024-00${i+1}`, supplierId: 'sup-001', warehouseId: 'wh-001',
      status: poStatuses[i],
      items: [{ skuId: 'sku-001', qty: 50, unitPrice: 18000000 }],
      totalAmount: 900000000,
      createdBy: 'user-admin-001', approvedBy: i >= 1 ? 'user-admin-001' : null,
      createdAt: admin.firestore.FieldValue.serverTimestamp(),
      updatedAt: admin.firestore.FieldValue.serverTimestamp(),
      note: `Đơn mẫu trạng thái ${poStatuses[i]}`
    });
  }

  // --- Sales Orders (SO) ---
  const soStatuses = ['Draft', 'Approved', 'Processing', 'Done', 'Cancelled'];
  for (let i = 0; i < soStatuses.length; i++) {
    await db.collection('salesOrders').doc(`so-00${i+1}`).set({
      soCode: `SO-2024-00${i+1}`, customerId: 'user-cus-001',
      status: soStatuses[i],
      items: [{ skuId: 'sku-001', qty: 2, unitPrice: 20000000 }],
      totalAmount: 40000000,
      shippingAddress: '123 Nguyễn Huệ, Q1, TP.HCM',
      createdBy: 'user-staff-001',
      createdAt: admin.firestore.FieldValue.serverTimestamp(),
      updatedAt: admin.firestore.FieldValue.serverTimestamp()
    });
  }

  console.log('Seed hoàn tất!');
  process.exit(0);
}

seed().catch(err => { console.error(err); process.exit(1); });