// ============================================================
// Configuration
// ============================================================
const API_BASE = '/api/planning';
const SLOT_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

// ============================================================
// Tab Navigation
// ============================================================
document.querySelectorAll('.tab-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    const target = btn.dataset.tab;
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById(`tab-${target}`).classList.add('active');
    if (target === 'history') loadHistory();
  });
});

// ============================================================
// Build Slot Inputs
// ============================================================
function buildSlots() {
  const container = document.getElementById('slotsContainer');
  container.innerHTML = '';
  SLOT_NAMES.forEach((name, i) => {
    const row = document.createElement('div');
    row.className = 'slot-row';
    row.innerHTML = `
      <span class="slot-label">${name}</span>
      <input type="number" id="slot-${i}" min="0" step="1"
             placeholder="0" value="0" aria-label="${name} quantity" />
    `;
    container.appendChild(row);
  });
}

buildSlots();

// ============================================================
// Client-side Validation
// ============================================================
function validateForm() {
  let valid = true;

  const requestCode = document.getElementById('requestCode').value.trim();
  const rcErr = document.getElementById('err-requestCode');
  if (!requestCode) {
    rcErr.textContent = 'Request Code is required.';
    valid = false;
  } else {
    rcErr.textContent = '';
  }

  const slotErr = document.getElementById('err-slots');
  const quantities = [];
  for (let i = 0; i < SLOT_NAMES.length; i++) {
    const raw = document.getElementById(`slot-${i}`).value;
    const num = Number(raw);
    if (raw === '' || isNaN(num)) {
      slotErr.textContent = `${SLOT_NAMES[i]}: value must be a number.`;
      valid = false;
      break;
    }
    if (num < 0) {
      slotErr.textContent = `${SLOT_NAMES[i]}: negative values are not allowed.`;
      valid = false;
      break;
    }
    if (!Number.isInteger(num)) {
      slotErr.textContent = `${SLOT_NAMES[i]}: fractional values are not allowed.`;
      valid = false;
      break;
    }
    quantities.push(num);
  }
  if (valid) slotErr.textContent = '';

  return { valid, quantities };
}

// ============================================================
// Submit Form
// ============================================================
document.getElementById('planningForm').addEventListener('submit', async (e) => {
  e.preventDefault();

  const { valid, quantities } = validateForm();
  if (!valid) return;

  const requestCode = document.getElementById('requestCode').value.trim();
  const submitBtn = document.getElementById('submitBtn');
  const btnText = submitBtn.querySelector('.btn-text');
  const spinner = submitBtn.querySelector('.btn-spinner');

  // Loading state
  submitBtn.disabled = true;
  btnText.textContent = 'Processing…';
  spinner.classList.remove('hidden');
  hideResult();

  const payload = {
    requestCode,
    slots: SLOT_NAMES.map((name, i) => ({
      slotName: name,
      quantity: quantities[i]
    }))
  };

  try {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    const data = await res.json();

    if (!res.ok) {
      showError(data.error || `Error ${res.status}`);
    } else {
      showResult(data);
    }
  } catch (err) {
    showError('Network error. Is the API running?');
  } finally {
    submitBtn.disabled = false;
    btnText.textContent = 'Balance & Save';
    spinner.classList.add('hidden');
  }
});

// ============================================================
// Render Result
// ============================================================
function showResult(data) {
  const panel = document.getElementById('resultPanel');
  const errorPanel = document.getElementById('errorPanel');
  errorPanel.classList.add('hidden');

  document.getElementById('res-requestCode').textContent = data.requestCode;
  document.getElementById('res-candidateToken').textContent = data.candidateToken;
  document.getElementById('res-createdAt').textContent = new Date(data.createdAt).toLocaleString();

  const statusBadge = document.getElementById('resultStatus');
  statusBadge.textContent = data.status;
  statusBadge.className = `badge ${data.status === 'Success' ? 'badge-success' : 'badge-error'}`;

  const tbody = document.getElementById('resultTableBody');
  tbody.innerHTML = '';
  data.slots.forEach(slot => {
    const delta = slot.balancedQuantity - slot.originalQuantity;
    const deltaClass = delta > 0 ? 'delta-positive' : delta < 0 ? 'delta-negative' : 'delta-zero';
    const deltaText = delta > 0 ? `+${delta}` : `${delta}`;
    const tr = document.createElement('tr');
    if (!slot.isActive) tr.classList.add('inactive-row');
    tr.innerHTML = `
      <td>${slot.slotOrder}</td>
      <td>${slot.slotName}</td>
      <td>${slot.originalQuantity}</td>
      <td>${slot.balancedQuantity}</td>
      <td class="${deltaClass}">${slot.isActive ? deltaText : '—'}</td>
      <td>${slot.isActive ? '<span style="color:green">Active</span>' : '<span style="color:gray">Inactive</span>'}</td>
    `;
    tbody.appendChild(tr);
  });

  document.getElementById('res-originalTotal').textContent = data.originalTotal;
  document.getElementById('res-balancedTotal').textContent = data.balancedTotal;
  document.getElementById('res-totalValid').innerHTML = data.isTotalValid
    ? '<span style="color:green">✓ Valid</span>'
    : '<span style="color:red">✗ Mismatch</span>';

  panel.classList.remove('hidden');
  panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function showError(message) {
  document.getElementById('resultPanel').classList.add('hidden');
  document.getElementById('errorMessage').textContent = message;
  document.getElementById('errorPanel').classList.remove('hidden');
}

function hideResult() {
  document.getElementById('resultPanel').classList.add('hidden');
  document.getElementById('errorPanel').classList.add('hidden');
}

// ============================================================
// History
// ============================================================
async function loadHistory() {
  const content = document.getElementById('historyContent');
  document.getElementById('detailPanel').classList.add('hidden');
  content.innerHTML = '<p class="empty-state">Loading…</p>';

  try {
    const res = await fetch(`${API_BASE}?page=1&pageSize=50`);
    const data = await res.json();

    if (!res.ok || !Array.isArray(data) || data.length === 0) {
      content.innerHTML = '<p class="empty-state">No submissions yet.</p>';
      return;
    }

    const table = document.createElement('table');
    table.className = 'history-table';
    table.innerHTML = `
      <thead>
        <tr>
          <th>Request Code</th>
          <th>Created At</th>
          <th>Active Slots</th>
          <th>Orig. Total</th>
          <th>Bal. Total</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody></tbody>
    `;

    const tbody = table.querySelector('tbody');
    data.forEach(item => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td><strong>${item.requestCode}</strong></td>
        <td>${new Date(item.createdAt).toLocaleString()}</td>
        <td>${item.activeSlotCount}</td>
        <td>${item.originalTotal}</td>
        <td>${item.balancedTotal}</td>
        <td><span class="badge ${item.status === 'Success' ? 'badge-success' : 'badge-error'}">${item.status}</span></td>
      `;
      tr.addEventListener('click', () => loadDetail(item.requestCode));
      tbody.appendChild(tr);
    });

    content.innerHTML = '';
    content.appendChild(table);
  } catch {
    content.innerHTML = '<p class="empty-state">Failed to load history.</p>';
  }
}

document.getElementById('refreshBtn').addEventListener('click', loadHistory);

// ============================================================
// Detail View
// ============================================================
async function loadDetail(requestCode) {
  const detailPanel = document.getElementById('detailPanel');
  detailPanel.classList.add('hidden');

  try {
    const res = await fetch(`${API_BASE}/${encodeURIComponent(requestCode)}`);
    if (!res.ok) return;
    const data = await res.json();

    document.getElementById('detail-requestCode').textContent = data.requestCode;
    document.getElementById('detail-candidateToken').textContent = data.candidateToken;
    document.getElementById('detail-createdAt').textContent = new Date(data.createdAt).toLocaleString();

    const statusBadge = document.getElementById('detail-status');
    statusBadge.textContent = data.status;
    statusBadge.className = `badge ${data.status === 'Success' ? 'badge-success' : 'badge-error'}`;

    const tbody = document.getElementById('detailTableBody');
    tbody.innerHTML = '';
    data.slots.forEach(slot => {
      const delta = slot.balancedQuantity - slot.originalQuantity;
      const deltaClass = delta > 0 ? 'delta-positive' : delta < 0 ? 'delta-negative' : 'delta-zero';
      const deltaText = delta > 0 ? `+${delta}` : `${delta}`;
      const tr = document.createElement('tr');
      if (!slot.isActive) tr.classList.add('inactive-row');
      tr.innerHTML = `
        <td>${slot.slotOrder}</td>
        <td>${slot.slotName}</td>
        <td>${slot.originalQuantity}</td>
        <td>${slot.balancedQuantity}</td>
        <td class="${deltaClass}">${slot.isActive ? deltaText : '—'}</td>
        <td>${slot.isActive ? '<span style="color:green">Active</span>' : '<span style="color:gray">Inactive</span>'}</td>
      `;
      tbody.appendChild(tr);
    });

    document.getElementById('detail-originalTotal').textContent = data.originalTotal;
    document.getElementById('detail-balancedTotal').textContent = data.balancedTotal;
    document.getElementById('detail-totalValid').innerHTML = data.isTotalValid
      ? '<span style="color:green">✓ Valid</span>'
      : '<span style="color:red">✗ Mismatch</span>';

    detailPanel.classList.remove('hidden');
    detailPanel.scrollIntoView({ behavior: 'smooth', block: 'start' });
  } catch {
    // fail silently
  }
}

document.getElementById('backToHistoryBtn').addEventListener('click', () => {
  document.getElementById('detailPanel').classList.add('hidden');
});
