const { ORDER_STATUS_TRANSITIONS } = require('../constants/statusEnum');

function validateOrderTransition(currentStatus, newStatus) {
  const allowed = ORDER_STATUS_TRANSITIONS[currentStatus] || [];
  if (!allowed.includes(newStatus)) {
    throw new Error(`Không thể chuyển từ "${currentStatus}" sang "${newStatus}"`);
  }
}

module.exports = { validateOrderTransition };