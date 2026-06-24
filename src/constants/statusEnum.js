// src/constants/statusEnum.js

const ORDER_STATUS = {
  DRAFT:      'Draft',
  APPROVED:   'Approved',
  PROCESSING: 'Processing',
  DONE:       'Done',
  CANCELLED:  'Cancelled',
};

const ORDER_STATUS_TRANSITIONS = {
  Draft:      ['Approved', 'Cancelled'],
  Approved:   ['Processing', 'Cancelled'],
  Processing: ['Done', 'Cancelled'],
  Done:       [],
  Cancelled:  [],
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

// Bộ vai trò thống nhất với backend .NET (PhanQuyen). MaVaiTro: 1=customer, 2=staff, 3=manager, 4=admin.
const USER_ROLE = {
  CUSTOMER: 'customer',
  STAFF:    'staff',
  MANAGER:  'manager',
  ADMIN:    'admin',
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