// src/constants/statusEnum.js

// Vòng đời đơn hàng theo luồng COD (thanh toán khi nhận hàng):
// Đặt hàng -> Đại lý xử lý/xác nhận -> Vận chuyển -> Giao tới -> Khách xác nhận
const ORDER_STATUS = {
  DA_DAT_HANG:     'Da dat hang',     // khách vừa đặt, tồn kho đã được khóa
  DANG_XU_LY:      'Dang xu ly',      // đại lý xác nhận, chuẩn bị hàng
  DANG_VAN_CHUYEN: 'Dang van chuyen', // đã xuất mã vận đơn, giao cho đơn vị vận chuyển
  DA_GIAO:         'Da giao',         // đơn vị vận chuyển giao tới khách
  HOAN_TAT:        'Hoan tat',        // khách xác nhận nhận hàng & thu tiền COD
  DA_HUY:          'Da huy',
};

const ORDER_STATUS_TRANSITIONS = {
  'Da dat hang':     ['Dang xu ly', 'Da huy'],
  'Dang xu ly':      ['Dang van chuyen', 'Da huy'],
  'Dang van chuyen': ['Da giao'],
  'Da giao':         ['Hoan tat'],
  'Hoan tat':        [],
  'Da huy':          [],
};

const GRN_STATUS = {
  DRAFT:    'Draft',
  APPROVED: 'Approved',
  DONE:     'Done',
};

const GRN_STATUS_TRANSITIONS = {
  Draft:    ['Approved'],
  Approved: ['Done'],
  Done:     [],
};

const GIN_STATUS = {
  DRAFT:    'Draft',
  APPROVED: 'Approved',
  DONE:     'Done',
};

const GIN_STATUS_TRANSITIONS = {
  Draft:    ['Approved'],
  Approved: ['Done'],
  Done:     [],
};

const SHIPMENT_STATUS = {
  PENDING:   'Pending',
  SHIPPED:   'Shipped',
  DELIVERED: 'Delivered',
  RETURNED:  'Returned',
};

const SHIPMENT_STATUS_TRANSITIONS = {
  Pending:   ['Shipped'],
  Shipped:   ['Delivered', 'Returned'],
  Delivered: [],
  Returned:  [],
};

const INVENTORY_TXN_TYPE = {
  IN:     'IN',
  OUT:    'OUT',
  ADJUST: 'ADJUST',
};

const USER_ROLE = {
  ADMIN:           'admin',
  WAREHOUSE_STAFF: 'warehouse_staff',
  SUPPLIER:        'supplier',
  CUSTOMER:        'customer',
};

// Helper: kiểm tra transition có hợp lệ không
function isValidTransition(transitionMap, currentStatus, nextStatus) {
  const allowed = transitionMap[currentStatus];
  if (!allowed) return false;
  return allowed.includes(nextStatus);
}

// Helper: lấy các trạng thái có thể chuyển tiếp
function getAllowedTransitions(transitionMap, currentStatus) {
  return transitionMap[currentStatus] || [];
}

module.exports = {
  ORDER_STATUS,
  ORDER_STATUS_TRANSITIONS,
  GRN_STATUS,
  GRN_STATUS_TRANSITIONS,
  GIN_STATUS,
  GIN_STATUS_TRANSITIONS,
  SHIPMENT_STATUS,
  SHIPMENT_STATUS_TRANSITIONS,
  INVENTORY_TXN_TYPE,
  USER_ROLE,
  isValidTransition,
  getAllowedTransitions,
};