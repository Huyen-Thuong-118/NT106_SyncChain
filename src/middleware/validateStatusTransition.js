const { ORDER_STATUS_TRANSITIONS } = require('../constants/statusEnum');

function validateOrderTransition(currentStatus, newStatus) {
  const allowed = ORDER_STATUS_TRANSITIONS[currentStatus] || [];
  if (!allowed.includes(newStatus)) {
    const err = new Error(`Không thể chuyển từ "${currentStatus}" sang "${newStatus}"`);
    err.status = 409;
    throw err;
  }
}

module.exports = { validateOrderTransition };