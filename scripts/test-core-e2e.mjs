const baseUrl = process.env.API_BASE_URL ?? "http://localhost:5292";
const email = process.env.TEST_ADMIN_EMAIL ?? "admin@gmail.com";
const password = process.env.TEST_ADMIN_PASSWORD ?? "123456";
const runId = new Date().toISOString().replace(/\D/g, "").slice(0, 14);

let token = "";
const results = [];

async function api(method, path, body, headers = {}) {
  const response = await fetch(`${baseUrl}/${path.replace(/^\//, "")}`, {
    method,
    headers: {
      ...(body ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  const text = await response.text();
  let data = text;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    // Plain text API response.
  }

  return { status: response.status, ok: response.ok, data };
}

function record(name, response, expected = [200, 201]) {
  const passed = expected.includes(response.status);
  results.push({ name, status: response.status, passed });
  console.log(`${passed ? "PASS" : "FAIL"} ${name}: HTTP ${response.status}`);
  if (!passed)
    console.log(JSON.stringify(response.data, null, 2));
  return response.data;
}

function requireValue(value, message) {
  if (value === undefined || value === null)
    throw new Error(message);
  return value;
}

async function main() {
  const login = await api("POST", "api/Auth/login", { email, password });
  const loginData = record("Login admin", login);
  token = requireValue(loginData.token, "Login response does not contain token.");

  record("Profile", await api("GET", "api/Auth/profile"));
  record("List internal users", await api("GET", "api/admin/users"));

  const testUser = record("Create staff", await api("POST", "api/admin/users", {
    email: `staff.${runId}@syncchain.test`,
    username: `staff-${runId}`,
    password: "123456",
    role: "staff",
  }));
  const userId = requireValue(testUser.maNguoiDung, "Missing user id.");
  record("Disable staff", await api("PUT", `api/admin/users/${userId}/active`, {
    isActive: false,
  }));
  record("Enable staff", await api("PUT", `api/admin/users/${userId}/active`, {
    isActive: true,
  }));

  const category = record("Create category", await api("POST", "api/category", {
    tenDanhMuc: `E2E-${runId}`,
    moTa: "Category created by automated end-to-end test",
  }));
  const categoryId = requireValue(category.maDanhMuc, "Missing category id.");
  record("List categories", await api("GET", "api/category"));

  const product = record("Create product", await api("POST", "api/product", {
    tenSanPham: `E2E Product ${runId}`,
    giaBan: 150000,
    giaNhap: 90000,
    soLuongTon: 10,
    hinhAnhUrl: "",
    moTa: "Product created by automated end-to-end test",
    maDanhMuc: categoryId,
  }));
  const productId = requireValue(product.maSanPham, "Missing product id.");
  record("Update product", await api("PUT", `api/product/${productId}`, {
    tenSanPham: `E2E Product ${runId} Updated`,
    giaBan: 155000,
    giaNhap: 90000,
    soLuongTon: 10,
    hinhAnhUrl: "",
    moTa: "Updated by end-to-end test",
    maDanhMuc: categoryId,
    trangThai: "Hoat dong",
  }));
  record("Product detail", await api("GET", `api/product/${productId}/detail`));

  record("Inventory adjustment", await api("POST", "api/inventory/adjustments", {
    maSanPham: productId,
    soLuongThayDoi: 2,
    lyDo: "E2E adjustment",
    ghiChu: "Automated test",
  }));
  record("Inventory reconcile preview", await api("POST", "api/inventory/reconcile", {
    applyFix: false,
  }));

  const receipt = record("Create warehouse receipt", await api("POST", "api/warehouse-receipts", {
    tenNguonNhap: `Factory ${runId}`,
    diaChiNguonNhap: "Localhost",
    nguoiLienHe: "E2E Test",
    ghiChu: "Automated receipt",
    chiTiet: [{ maSanPham: productId, soLuong: 3, donGiaNhap: 90000 }],
  }), [201]);
  const receiptId = requireValue(receipt.maPhieuNhap, "Missing receipt id.");
  record("Submit receipt", await api("PUT", `api/warehouse-receipts/${receiptId}/submit`));
  record("Approve receipt", await api("PUT", `api/warehouse-receipts/${receiptId}/approve`));
  record("Complete receipt transaction", await api("PUT", `api/warehouse-receipts/${receiptId}/complete`));

  const issue = record("Create warehouse issue", await api("POST", "api/warehouse-issues", {
    lyDoXuat: "E2E issue",
    ghiChu: "Automated issue",
    chiTiet: [{ maSanPham: productId, soLuong: 1 }],
  }), [201]);
  const issueId = requireValue(issue.maPhieuXuat, "Missing issue id.");
  record("Submit issue", await api("PUT", `api/warehouse-issues/${issueId}/submit`));
  record("Complete issue transaction", await api("PUT", `api/warehouse-issues/${issueId}/complete`));

  const orderKey = `e2e-order-${runId}`;
  const order = record("Create order and lock inventory", await api("POST", "api/order", {
    items: [{ maSanPham: productId, soLuong: 1 }],
    idempotencyKey: orderKey,
  }, { "Idempotency-Key": orderKey }));
  const orderId = requireValue(order.maDonHang, "Missing order id.");
  const orderStatus = record("Update order status with version", await api(
    "PUT",
    `api/order/${orderId}/status`,
    { status: "processing", expectedStatus: "pending", concurrencyVersion: 0 },
  ));

  const shipping = record("Create shipping", await api("POST", `api/orders/${orderId}/shipping`, {
    carrier: "SyncChain Express",
    trackingNumber: `TRACK-${runId}`,
    shippingFee: 30000,
    estimatedDeliveryAt: new Date(Date.now() + 3 * 86400000).toISOString(),
  }));
  record("Update shipping status with version", await api(
    "PUT",
    `api/orders/${orderId}/shipping/status`,
    {
      status: "ready",
      expectedStatus: "pending",
      concurrencyVersion: shipping.concurrencyVersion,
      note: "Ready from E2E test",
    },
  ));
  record("Shipping history", await api("GET", `api/orders/${orderId}/shipping/history`));

  const oversellProduct = record("Create oversell test product", await api("POST", "api/product", {
    tenSanPham: `Oversell Product ${runId}`,
    giaBan: 100000,
    giaNhap: 50000,
    soLuongTon: 5,
    hinhAnhUrl: "",
    moTa: "Concurrency test product",
    maDanhMuc: categoryId,
  }));
  const oversellProductId = requireValue(oversellProduct.maSanPham, "Missing oversell product id.");
  const oversellA = `oversell-a-${runId}`;
  const oversellB = `oversell-b-${runId}`;
  const oversellResponses = await Promise.all([
    api("POST", "api/order", {
      items: [{ maSanPham: oversellProductId, soLuong: 4 }],
      idempotencyKey: oversellA,
    }, { "Idempotency-Key": oversellA }),
    api("POST", "api/order", {
      items: [{ maSanPham: oversellProductId, soLuong: 4 }],
      idempotencyKey: oversellB,
    }, { "Idempotency-Key": oversellB }),
  ]);
  const oversellSuccesses = oversellResponses.filter((x) => x.ok).length;
  const stockAfterOversell = await api("GET", `api/inventory/products/${oversellProductId}`);
  const oversellPassed =
    oversellSuccesses === 1 &&
    stockAfterOversell.ok &&
    stockAfterOversell.data.soLuongTon >= 0;
  results.push({ name: "Concurrent oversell protection", status: oversellPassed ? 200 : 500, passed: oversellPassed });
  console.log(`${oversellPassed ? "PASS" : "FAIL"} Concurrent oversell protection: successes=${oversellSuccesses}, stock=${stockAfterOversell.data.soLuongTon}`);

  const conflictKey = `conflict-${runId}`;
  const conflictOrder = record("Create conflict test order", await api("POST", "api/order", {
    items: [{ maSanPham: productId, soLuong: 1 }],
    idempotencyKey: conflictKey,
  }, { "Idempotency-Key": conflictKey }));
  const conflictOrderId = requireValue(conflictOrder.maDonHang, "Missing conflict order id.");
  const conflictResponses = await Promise.all([
    api("PUT", `api/order/${conflictOrderId}/status`, {
      status: "processing",
      expectedStatus: "pending",
      concurrencyVersion: 0,
    }),
    api("PUT", `api/order/${conflictOrderId}/status`, {
      status: "cancel",
      expectedStatus: "pending",
      concurrencyVersion: 0,
    }),
  ]);
  const conflictSuccesses = conflictResponses.filter((x) => x.ok).length;
  const conflictPassed = conflictSuccesses === 1;
  results.push({ name: "Order concurrency conflict", status: conflictPassed ? 200 : 500, passed: conflictPassed });
  console.log(`${conflictPassed ? "PASS" : "FAIL"} Order concurrency conflict: successes=${conflictSuccesses}, statuses=${conflictResponses.map((x) => x.status).join(",")}`);

  record("Dashboard report", await api("GET", "api/reports/dashboard"));
  record("Revenue report", await api("GET", "api/reports/revenue"));
  record("Inventory report", await api("GET", "api/reports/inventory"));
  record("Shipping report", await api("GET", "api/reports/shipping"));
  record("Audit logs", await api("GET", "api/audit-logs?pageSize=100"));

  await api("POST", "api/inventory/adjustments", {
    maSanPham: -1,
    soLuongThayDoi: 1,
    lyDo: "Generate system error log",
    ghiChu: "Expected failure",
  });
  record("System error logs", await api("GET", "api/system-error-logs?pageSize=100"));

  const failed = results.filter((x) => !x.passed);
  console.log(`\nSummary: ${results.length - failed.length}/${results.length} checks passed.`);
  if (failed.length) {
    console.log(`Failed: ${failed.map((x) => x.name).join(", ")}`);
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
